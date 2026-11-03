# Unity 设置

1. Unity Hub 安装 Unity 6。
2. 用 Unity Hub 直接打开仓库中的 `UnityProject/` 文件夹。
3. 场景中创建空 GameObject，命名 DouniApp，挂载 `DouniDemo.cs`。
4. 确认场景有 EventSystem（没有则 GameObject > UI > Event System）。
5. 在终端进入仓库 `proxy/`，运行 `npm install`，然后 `npm start`。
6. 运行 Unity 场景，在应用顶部的密码框输入 DeepSeek API Key。

API Key 仅保存在运行时内存，不写入 PlayerPrefs、文件或日志；退出应用后清空。每次请求只带 system prompt 和当前问题，不启用对话记忆。聊天记录只在本次运行中临时显示。

若字体不支持中文，请导入支持简体中文的字体替换 UI 字体。正式展示前请检查不同分辨率、网络错误和 API 配额。
