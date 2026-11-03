import express from "express";
import cors from "cors";
const app=express(),port=Number(process.env.PORT||3000);
app.disable("x-powered-by");
app.use(cors({origin:true,methods:["GET","POST"],allowedHeaders:["Content-Type","x-deepseek-api-key"]}));
app.use(express.json({limit:"64kb"}));
app.get("/health",(_q,res)=>{res.set("Cache-Control","no-store");res.json({ok:true,service:"douni-local-proxy"});});
app.post("/chat",async(req,res)=>{
 res.set("Cache-Control","no-store");
 const key=String(req.get("x-deepseek-api-key")||"").trim();
 if(!key)return res.status(400).json({error:"请先在应用界面输入 DeepSeek API Key"});
 const messages=req.body?.messages;
 if(!Array.isArray(messages)||messages.length<2||messages.length>22)return res.status(400).json({error:"消息数量无效，请清空较早的聊天记录后重试"});
 for(const m of messages){if(!m||!["system","user","assistant"].includes(m.role)||typeof m.content!=="string"||m.content.length>12000)return res.status(400).json({error:"消息格式无效或内容过长"});}
 const stream=req.body.stream!==false,controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),90000);
 res.on("close",()=>{if(!res.writableEnded)controller.abort();});
 try{
  const upstream=await fetch("https://api.deepseek.com/chat/completions",{method:"POST",headers:{Authorization:"Bearer "+key,"Content-Type":"application/json"},body:JSON.stringify({model:"deepseek-chat",messages,stream,temperature:.7}),signal:controller.signal});
  if(!upstream.ok){
   const detail=await upstream.text();let message="DeepSeek 请求失败";
   if(upstream.status===401||upstream.status===403)message="API Key 无效或没有访问权限";
   else if(upstream.status===402)message="账户余额不足或额度不可用";
   else if(upstream.status===429)message="请求过于频繁，请稍后再试";
   else if(upstream.status>=500)message="DeepSeek 服务暂时不可用，请稍后重试";
   return res.status(upstream.status).json({error:message,status:upstream.status,detail:detail.slice(0,1200)});
  }
  if(!stream){res.type("application/json").send(await upstream.text());return;}
  res.status(200).set({"Content-Type":"text/event-stream; charset=utf-8","Cache-Control":"no-cache, no-transform","Connection":"keep-alive","X-Accel-Buffering":"no"});
  const reader=upstream.body.getReader();
  try{while(true){const {value,done}=await reader.read();if(done)break;if(!res.write(value))await new Promise(resolve=>res.once("drain",resolve));}}
  finally{reader.releaseLock();if(!res.writableEnded)res.end();}
 }catch(e){
  if(!res.headersSent)res.status(e?.name==="AbortError"?504:502).json({error:e?.name==="AbortError"?"请求超时或已取消，请重试":"无法连接 DeepSeek 服务，请检查网络"});
  else if(!res.writableEnded)res.end();
 }finally{clearTimeout(timeout);}
});
app.listen(port,"127.0.0.1",()=>console.log("Douni local proxy listening on http://127.0.0.1:"+port));
