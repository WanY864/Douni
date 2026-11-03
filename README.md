# 小豆泥 AI 聊天实验

Unity 6 + DeepSeek API 双 NPC 2D 聊天原型。红围脖猫在左、蓝围脖猫在右；角色无嘴巴，眼睛位置贴近参考图。当前说话角色的耳朵轻轻摆动。

## 记忆与隐私
- 本地用户记忆默认开启：红猫和蓝猫分别保存聊天历史到本机 `Application.persistentDataPath/douni-user-memory.json`。
- 记忆由 Unity 应用管理，不使用 DeepSeek 云端记忆功能。每次请求由应用明确发送 NPC 人设和最近最多 20 条对话消息。
- 游戏内可关闭本地记忆（同时删除本地记忆文件），也可清空当前 NPC 的聊天记忆。历史最多保留 80 条/角色。
- DeepSeek API Key 在游戏内密码框输入，只存在运行时内存，不写入 PlayerPrefs、文件、日志或 Git；退出时清空。
- CSV 仅记录实验条件和响应时间，不记录聊天内容或 API Key。

## 启动
1. 安装 Node.js 18+，进入 `proxy/` 执行 `npm install`，再运行 `npm start`。
2. 用 Unity 6 打开 `UnityProject/`。
3. 场景中新建空物体挂载 `DouniDemo.cs`，Play。
4. 在游戏内输入 DeepSeek API Key。

## 异常处理
代理会检查 API Key、消息格式和长度，处理 401/403、余额不足、429、服务端错误、网络断开和 90 秒超时。Unity 请求也有 90 秒超时，并对无效响应、空流式回复和 CSV 写入失败显示提示。遇到失败时可检查本地代理终端、网络、Key 权限和 DeepSeek 额度。

本地代理只监听 127.0.0.1，不要暴露公网。实验流程见 `docs/EXPERIMENT.md`。本仓库尚需在本机 Unity Editor 实际编译运行验证。
