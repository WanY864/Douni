# 小豆泥 AI 聊天实验

Unity 6 + DeepSeek API 的双 NPC 2D 聊天原型。红围脖猫在左、蓝围脖猫在右；两只角色同时显示、没有嘴巴。眼睛沿用用户原图的位置，仅略微放大。当前发言角色的耳朵轻轻摆动，另一只保持静止。

## 功能
- 双 NPC 人格设定；每次请求仅发送人格提示和当前问题，不保留跨轮对话记忆
- DeepSeek API Key 在游戏界面手动输入，密码框显示；只保存在当前运行内存，退出应用即清空
- API Key 通过本机代理的请求头临时传递，不写入文件、PlayerPrefs、日志或 Git
- SSE Streaming / 一次性回复切换，Typing Indicator 开关
- CSV 记录首 token 延迟、总响应时间、NPC 和实验条件（不记录 API Key 或聊天内容）

## 启动
1. 安装 Node.js 18+。
2. 在 `proxy/` 执行 `npm install`，然后 `npm start`（默认监听 127.0.0.1:3000）。
3. 用 Unity 6 打开 `UnityProject/`。
4. 在场景中创建空物体，挂载 `DouniDemo.cs`，Play。
5. 在游戏界面上方的密码输入框粘贴自己的 DeepSeek API Key；退出应用后需要重新输入。

## 隐私说明
本应用没有长期记忆功能。每次 API 请求只发送当前 NPC 的固定人格提示和用户当前这一条消息；应用内对话仅为当前运行期间显示，退出后清空。API Key 不会被保存到本地文件或 PlayerPrefs。CSV 仅记录实验时间和响应延迟，不包含聊天文本、身份信息或密钥。

本地代理只监听 127.0.0.1，不要直接暴露公网。实验流程见 `docs/EXPERIMENT.md`。请实际招募受试者并填写数据，不要将模拟数据当作实验结果。
