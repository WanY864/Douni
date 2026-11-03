# 设置与隐私

1. Unity Hub 安装 Unity 6，打开仓库中的 `UnityProject/`。
2. 场景中新建空 GameObject，挂载 `DouniDemo.cs`；确保有 EventSystem。
3. 在终端进入 `proxy/`，执行 `npm install` 和 `npm start`。
4. 运行 Unity，在密码框输入 DeepSeek API Key。

## 用户记忆
本地记忆默认开启，红、蓝 NPC 分别保留最近对话，存放在 Unity 的 persistentDataPath。API Key 不进入此文件。游戏内“本地记忆”开关关闭时会删除本地记忆文件；“清空本轮”清除当前角色的历史。请求只携带当前 NPC 人设和最近最多 20 条消息，不使用供应商端记忆服务。

请注意：本地记忆是普通本机文件，不是加密保险库。若多人共用电脑，请关闭记忆或清除历史。API Key 在程序运行时短暂存在内存，退出时清空。
