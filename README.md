# 小豆泥 AI 聊天实验

Unity 6 + DeepSeek API 双 NPC 2D 聊天原型。红围脖猫在左、蓝围脖猫在右；角色无嘴巴，眼睛位置贴近参考图。当前说话角色的耳朵轻轻摆动。

## 记忆与隐私
- **本地用户记忆默认开启**：红猫和蓝猫分别保存聊天历史到本机 `Application.persistentDataPath/douni-user-memory.json`，下次启动可继续上下文。
- 记忆由 Unity 应用管理，不使用 DeepSeek 的云端记忆功能。每次请求由应用明确发送系统人设和最近最多 20 条对话消息。
- 游戏内可以关闭本地记忆（同时删除本地记忆文件），也可以清空当前 NPC 的记忆。
- DeepSeek API Key 在游戏内密码框输入，只存在运行时内存，不写入 PlayerPrefs、文件、日志或 Git；退出时清空。
- CSV 仅记录实验条件和响应时间，不记录聊天内容或 API Key。

## 启动
1. 安装 Node.js 18+，进入 `proxy/` 执行 `npm install`，再运行 `npm start`。
2. 用 Unity 6 打开 `UnityProject/`。
3. 场景中新建空物体挂载 `DouniDemo.cs`，Play。
4. 在游戏内输入 DeepSeek API Key。

本地代理只监听 127.0.0.1，不要暴露公网。实验流程见 `docs/EXPERIMENT.md`。
