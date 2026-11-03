# 小豆泥 AI 聊天实验

Unity 6 + DeepSeek API 的双 NPC 2D 聊天原型。红围脖猫在左、蓝围脖猫在右；两只角色同时显示、没有嘴巴。眼睛沿用用户原图的位置，仅略微放大。当前发言角色的耳朵轻轻摆动，另一只保持静止。

## 功能
- 两个独立人格与聊天历史
- DeepSeek Chat Completions，经本地 Node 代理接入
- SSE Streaming / 一次性回复切换
- Typing Indicator 开关
- CSV 记录首 token 延迟、总响应时间、NPC 和实验条件
- 角色由 Unity C# 运行时绘制，无需额外美术插件

## 启动
1. 安装 Node.js 18+。
2. 在 `proxy/` 执行 `npm install`。
3. 复制根目录 `.env.example` 为 `proxy/.env`，填写 DeepSeek API Key。
4. 在 `proxy/` 执行 `npm start`（默认 localhost:3000）。
5. 用 Unity 6 创建 2D 项目，把 `UnityProject/Assets` 复制进项目的 Assets。
6. 新建空物体并挂载 `DouniDemo.cs`，Play。

API Key 只保存在本机 `proxy/.env`，不要提交到 Git 或发到聊天。代理只监听 127.0.0.1，适合本机演示，不要直接暴露公网。

实验流程见 `docs/EXPERIMENT.md`。请实际招募受试者并填写数据，不要将模拟数据当作实验结果。
