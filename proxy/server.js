import express from "express";
import cors from "cors";

const app = express();
const port = Number(process.env.PORT || 3000);
const MAX_MESSAGES = 22;
const MAX_MESSAGE_CHARS = 12000;

app.disable("x-powered-by");
app.use(cors({ origin: "http://localhost", methods: ["GET", "POST"], allowedHeaders: ["Content-Type", "x-deepseek-api-key"] }));
app.use(express.json({ limit: "64kb" }));

app.get("/health", (_req, res) => {
  res.set("Cache-Control", "no-store");
  res.json({ ok: true, service: "douni-local-proxy" });
});

app.post("/chat", async (req, res) => {
  res.set("Cache-Control", "no-store");
  const apiKey = String(req.get("x-deepseek-api-key") || "").trim();
  if (!apiKey) return res.status(400).json({ error: "请先在应用界面输入 DeepSeek API Key" });
  if (!Array.isArray(req.body?.messages) || req.body.messages.length < 2 || req.body.messages.length > MAX_MESSAGES)
    return res.status(400).json({ error: "消息数量无效，请清空较早的聊天记录后重试" });

  const messages = req.body.messages;
  for (const message of messages) {
    if (!message || !["system", "user", "assistant"].includes(message.role) ||
        typeof message.content !== "string" || message.content.length > MAX_MESSAGE_CHARS) {
      return res.status(400).json({ error: "消息格式无效或内容过长" });
    }
  }

  const stream = req.body.stream !== false;
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 90000);
  req.on("close", () => { if (!res.writableEnded) controller.abort(); });

  try {
    const upstream = await fetch("https://api.deepseek.com/chat/completions", {
      method: "POST",
      headers: { Authorization: "Bearer " + apiKey, "Content-Type": "application/json" },
      body: JSON.stringify({
        model: "deepseek-chat",
        messages,
        stream,
        temperature: 0.7
      }),
      signal: controller.signal
    });

    if (!upstream.ok) {
      const detail = await upstream.text();
      let message = "DeepSeek 请求失败";
      if (upstream.status === 401 || upstream.status === 403) message = "API Key 无效或没有访问权限";
      else if (upstream.status === 402) message = "DeepSeek 账户余额不足或额度不可用";
      else if (upstream.status === 429) message = "请求过于频繁，请稍后再试";
      else if (upstream.status >= 500) message = "DeepSeek 服务暂时不可用，请稍后重试";
      return res.status(upstream.status).json({ error: message, status: upstream.status, detail: detail.slice(0, 1200) });
    }

    if (!stream) {
      res.type("application/json").send(await upstream.text());
      return;
    }

    res.status(200);
    res.set({
      "Content-Type": "text/event-stream; charset=utf-8",
      "Cache-Control": "no-cache, no-transform",
      "Connection": "keep-alive",
      "X-Accel-Buffering": "no"
    });
    const reader = upstream.body.getReader();
    try {
      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        if (!res.write(value)) await new Promise(resolve => res.once("drain", resolve));
      }
    } finally {
      reader.releaseLock();
      if (!res.writableEnded) res.end();
    }
  } catch (error) {
    if (!res.headersSent) {
      const timedOut = error?.name === "AbortError";
      res.status(timedOut ? 504 : 502).json({
        error: timedOut ? "请求超时或客户端已取消，请重试" : "无法连接 DeepSeek 服务，请检查网络",
      });
    } else if (!res.writableEnded) {
      res.end();
    }
  } finally {
    clearTimeout(timeout);
  }
});

app.listen(port, "127.0.0.1", () => {
  console.log("Douni local proxy listening on http://127.0.0.1:" + port);
});
