using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[Serializable] public class DouniMsg { public string role; public string content; public DouniMsg(string r,string c){role=r;content=c;} }
[Serializable] class DouniReq { public DouniMsg[] messages; public bool stream; }
[Serializable] class DouniDelta { public string content; }
[Serializable] class DouniChoice { public DouniDelta delta; public DouniMsg message; }
[Serializable] class DouniResp { public DouniChoice[] choices; }
[Serializable] class DouniMemoryData { public List<DouniMsg> red=new List<DouniMsg>(); public List<DouniMsg> blue=new List<DouniMsg>(); }

public class DouniDemo : MonoBehaviour {
 [Header("实验条件")] public bool streaming=true; public bool typingIndicator=true;
 public string proxyUrl="http://127.0.0.1:3000/chat";
 Text log, state; InputField entry, apiKeyInput; Button sendBtn, clearChatBtn, streamModeBtn, typingModeBtn, memoryModeBtn; int active=0; bool busy=false;
 readonly List<DouniMsg>[] history={new List<DouniMsg>(),new List<DouniMsg>()};
 readonly string[] names={"红围脖小豆泥","蓝围脖小豆泥"};
 readonly string[] prompts={
 "你是红围脖小豆泥，活泼、热情、爱鼓励人，中文回答自然可爱，尽量简短。",
 "你是蓝围脖小豆泥，冷静、温柔、耐心，善于解释问题，中文回答清楚简洁。"
 };
 bool localMemoryEnabled=true; string memoryPath;
 CatEar redEar,blueEar; float started,first=-1; string currentReply="";
 void Start(){
  memoryPath=Path.Combine(Application.persistentDataPath,"douni-user-memory.json");
  LoadMemory(); EnsureSystemPrompts();
  BuildUI();
  var cam=Camera.main;if(cam){cam.orthographic=true;cam.orthographicSize=5.2f;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(1f,.94f,.86f);}
  MakeCat(new Vector2(-4.6f,-.15f),new Color(.82f,.18f,.2f),out redEar);
  MakeCat(new Vector2(4.6f,-.15f),new Color(.2f,.43f,.8f),out blueEar); Select(0);
 }
 void BuildUI(){
  var c=new GameObject("DouniCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
  c.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;c.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1440,900);
  var panel=Rect(c.transform,"ChatPanel",new Vector2(0,0),new Vector2(700,690),new Color(1,.96f,.9f,.97f));
  Button red=Btn(c.transform,"红围脖小豆泥",new Vector2(-170,300),new Vector2(250,58),()=>Select(0));
  Button blue=Btn(c.transform,"蓝围脖小豆泥",new Vector2(170,300),new Vector2(250,58),()=>Select(1));
  apiKeyInput=Input(c.transform,new Vector2(0,235),new Vector2(560,42));apiKeyInput.inputType=InputField.InputType.Password;apiKeyInput.placeholder=Txt(apiKeyInput.transform,"在这里输入 DeepSeek API Key（仅本次运行使用）",Vector2.zero,new Vector2(540,40),16,TextAnchor.MiddleLeft);
  log=Txt(c.transform,"选择一位小豆泥，开始聊天吧！",new Vector2(0,5),new Vector2(620,450),24,TextAnchor.UpperLeft);
  streamModeBtn=Btn(c.transform,"流式输出：开",new Vector2(-155,-250),new Vector2(220,42),ToggleStream);
  typingModeBtn=Btn(c.transform,"输入提示：开",new Vector2(155,-250),new Vector2(220,42),ToggleTyping);
  memoryModeBtn=Btn(c.transform,"本地记忆：开",new Vector2(0,-205),new Vector2(220,38),ToggleMemory);
  state=Txt(c.transform,"本地代理未检测",new Vector2(0,-285),new Vector2(620,26),15,TextAnchor.MiddleCenter);
  entry=Input(c.transform,new Vector2(-70,-320),new Vector2(470,58));
  sendBtn=Btn(c.transform,"发送",new Vector2(255,-320),new Vector2(120,58),Send);\n  clearChatBtn=Btn(c.transform,"清空本轮",new Vector2(0,-365),new Vector2(160,42),ClearCurrentChat);
 }
 GameObject Rect(Transform parent,string n,Vector2 pos,Vector2 size,Color color){
  var g=new GameObject(n,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);
  var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;g.GetComponent<Image>().color=color;return g;
 }
 Text Txt(Transform p,string s,Vector2 pos,Vector2 size,int fs,TextAnchor a){
  var g=new GameObject("Text",typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
  var t=g.GetComponent<Text>();t.text=s;t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=fs;t.color=new Color(.25f,.2f,.17f);t.alignment=a;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
 }
 Button Btn(Transform p,string label,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action){
  var g=Rect(p,label,pos,size,new Color(.88f,.42f,.34f));var b=g.AddComponent<Button>();b.onClick.AddListener(action);var t=Txt(g.transform,label,Vector2.zero,size,21,TextAnchor.MiddleCenter);t.color=Color.white;return b;
 }
 InputField Input(Transform p,Vector2 pos,Vector2 size){
  var g=Rect(p,"MessageInput",pos,size,Color.white);var f=g.AddComponent<InputField>();
  var t=Txt(g.transform,"",Vector2.zero,size,20,TextAnchor.MiddleLeft);t.color=new Color(.2f,.18f,.16f);f.textComponent=t;f.targetGraphic=g.GetComponent<Image>();f.lineType=InputField.LineType.MultiLineNewline;return f;
 }
 void MakeCat(Vector2 pos,Color scarf,out CatEar ears){
  var go=new GameObject("小豆泥猫咪");go.transform.SetParent(transform,false);go.transform.position=new Vector3(pos.x,pos.y,0);go.transform.localScale=Vector3.one*.82f;
  var sr=go.AddComponent<SpriteRenderer>();sr.sprite=BuildCatSprite(scarf);sr.sortingOrder=0;
  var earLeft=MakeEar(go.transform,new Vector3(-.48f,1.02f,0),-8f);
  var earRight=MakeEar(go.transform,new Vector3(.48f,1.02f,0),8f);
  ears=go.AddComponent<CatEar>();ears.left=earLeft;ears.right=earRight;
 }
 Sprite BuildCatSprite(Color scarf){
  int w=240,h=280;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var px=new Color[w*h];Color clear=new Color(0,0,0,0),cream=new Color(1,.94f,.87f),brown=new Color(.62f,.52f,.42f),black=new Color(.04f,.04f,.04f),eyeBlue=new Color(.55f,.82f,.9f);
  for(int i=0;i<px.Length;i++)px[i]=clear;
  Ell(px,w,h,120,105,78,86,cream);Ell(px,w,h,120,184,104,68,cream);
  
  Ell(px,w,h,42,112,24,26,brown);Ell(px,w,h,198,112,24,26,brown);
  Ell(px,w,h,120,83,83,22,scarf);
  // Eyes are kept at the original reference height; only slightly enlarged. No mouth is drawn.
  Ell(px,w,h,82,185,29,32,eyeBlue);Ell(px,w,h,158,185,29,32,eyeBlue);
  Ell(px,w,h,82,185,23,26,black);Ell(px,w,h,158,185,23,26,black);
  Ell(px,w,h,89,197,6,7,Color.white);Ell(px,w,h,165,197,6,7,Color.white);
  Ell(px,w,h,120,181,17,19,brown);
  tex.SetPixels(px);tex.Apply();return Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);
 }
 Transform MakeEar(Transform parent,Vector3 pos,float angle){
  var g=new GameObject("EarPivot");g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,0,angle);
  var sr=g.AddComponent<SpriteRenderer>();sr.sprite=EarSprite();sr.sortingOrder=1;return g.transform;
 }
 Sprite EarSprite(){
  int w=64,h=90;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var px=new Color[w*h];Color clear=new Color(0,0,0,0),brown=new Color(.62f,.52f,.42f),ink=new Color(.04f,.04f,.04f);
  for(int i=0;i<px.Length;i++)px[i]=clear;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){float nx=(x-32f)/29f,ny=(y-8f)/78f;if(ny>=0&&ny<=1&&Mathf.Abs(nx)<(1-ny)*.75f)px[y*w+x]=brown;}
  tex.SetPixels(px);tex.Apply();return Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.08f),100);
 }
 void Ell(Color[] p,int w,int h,float cx,float cy,float rx,float ry,Color c){
  for(int y=Mathf.Max(0,(int)(cy-ry));y<Mathf.Min(h,(int)(cy+ry));y++)for(int x=Mathf.Max(0,(int)(cx-rx));x<Mathf.Min(w,(int)(cx+rx));x++)if(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<=1)p[y*w+x]=c;
 }
 void Select(int i){if(busy)return;active=i;Refresh();redEar.SetTalking(i==0&&busy);blueEar.SetTalking(i==1&&busy);}
 void Refresh(){string s="";for(int i=0;i<history[active].Count;i++){var m=history[active][i];if(m.role!="system")s+=(m.role=="user"?"你：":names[active]+"：")+m.content+"\n\n";}log.text=s; if(log.text.Length==0)log.text="你好呀！我是"+names[active]+"，想聊点什么？";}
 void ToggleStream(){if(busy)return;streaming=!streaming;streamModeBtn.GetComponentInChildren<Text>().text="流式输出："+(streaming?"开":"关");}
 void ToggleTyping(){if(busy)return;typingIndicator=!typingIndicator;typingModeBtn.GetComponentInChildren<Text>().text="输入提示："+(typingIndicator?"开":"关");}
 void ClearCurrentChat(){if(busy)return;history[active].Clear();history[active].Add(new DouniMsg("system",prompts[active]));Refresh();state.text="当前角色的临时聊天记录已清空";}\n void ToggleMemory(){if(busy)return;localMemoryEnabled=!localMemoryEnabled;memoryModeBtn.GetComponentInChildren<Text>().text="本地记忆："+(localMemoryEnabled?"开":"关");if(!localMemoryEnabled){DeleteMemoryFile();history[0].Clear();history[1].Clear();EnsureSystemPrompts();Refresh();state.text="本地记忆已关闭并清除";}else{SaveMemory();state.text="本地记忆已开启（仅保存在本机）";}}
 void EnsureSystemPrompts(){for(int i=0;i<2;i++){if(history[i].Count==0||history[i][0].role!="system")history[i].Insert(0,new DouniMsg("system",prompts[i]));}}
 void LoadMemory(){if(!File.Exists(memoryPath))return;try{var data=JsonUtility.FromJson<DouniMemoryData>(File.ReadAllText(memoryPath,Encoding.UTF8));if(data!=null){history[0]=data.red??new List<DouniMsg>();history[1]=data.blue??new List<DouniMsg>();}}catch{history[0].Clear();history[1].Clear();}}
 void SaveMemory(){if(!localMemoryEnabled)return;try{var data=new DouniMemoryData{red=history[0],blue=history[1]};File.WriteAllText(memoryPath,JsonUtility.ToJson(data,true),Encoding.UTF8);}catch(Exception e){Debug.LogWarning("Douni local memory save failed: "+e.Message);}}
 void DeleteMemoryFile(){try{if(File.Exists(memoryPath))File.Delete(memoryPath);}catch(Exception e){Debug.LogWarning("Douni memory delete failed: "+e.Message);}}
 void Send(){if(busy||string.IsNullOrWhiteSpace(entry.text))return;string q=entry.text.Trim();entry.text="";history[active].Add(new DouniMsg("user",q));SaveMemory();Refresh();StartCoroutine(RequestReply(active));}
 IEnumerator RequestReply(int npc){
  busy=true;started=Time.realtimeSinceStartup;first=-1;currentReply="";redEar.SetTalking(npc==0);blueEar.SetTalking(npc==1);sendBtn.interactable=false;
  if(typingIndicator)state.text=names[npc]+" 正在思考…";else state.text=" ";
  string apiKey=apiKeyInput.text.Trim();
  if(string.IsNullOrEmpty(apiKey)){state.text="请先输入 DeepSeek API Key";busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}
  // App-managed memory: send the recent local chat context explicitly; no provider memory is used.
  var context=new List<DouniMsg>();context.Add(new DouniMsg("system",prompts[npc]));
  int firstContext=Math.Max(1,history[npc].Count-20);
  for(int i=firstContext;i<history[npc].Count;i++)context.Add(history[npc][i]);
  var reqBody=JsonUtility.ToJson(new DouniReq{messages=context.ToArray(),stream=streaming});
  using(var req=new UnityWebRequest(proxyUrl,"POST")){
   req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(reqBody));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");req.SetRequestHeader("x-deepseek-api-key",apiKey);
   var op=req.SendWebRequest();int read=0;
   while(!op.isDone){
    if(streaming){string all=req.downloadHandler.text;if(all.Length>read){string fresh=all.Substring(read);read=all.Length;ParseSSE(fresh);}}
    yield return null;
   }
   if(req.result!=UnityWebRequest.Result.Success){state.text="连接失败："+req.error;busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}
   if(streaming){ParseSSE(req.downloadHandler.text.Substring(Mathf.Min(read,req.downloadHandler.text.Length)));}
   else{try{var response=JsonUtility.FromJson<DouniResp>(req.downloadHandler.text);currentReply=response.choices[0].message.content;}catch{currentReply="回复解析失败，请检查代理服务。";}}
  }
  if(first<0)first=Time.realtimeSinceStartup;history[npc].Add(new DouniMsg("assistant",currentReply));SaveMemory();SaveMemory();
  if(!streaming)Refresh();else Refresh();
  LogResult(npc,Time.realtimeSinceStartup-started,(first-started)*1000f);
  busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);state.text="完成 · 首字延迟 "+((first-started)*1000f).ToString("F0")+" ms";
 }
 string sseRemainder="";
 void ParseSSE(string chunk){
  sseRemainder+=chunk;
  int newline;
  while((newline=sseRemainder.IndexOf('\n'))>=0){
   string line=sseRemainder.Substring(0,newline).TrimEnd('\r');
   sseRemainder=sseRemainder.Substring(newline+1);
   if(!line.StartsWith("data: "))continue;
   string data=line.Substring(6).Trim();
   if(data=="[DONE]")continue;
   try{
    var r=JsonUtility.FromJson<DouniResp>(data);
    if(r.choices!=null&&r.choices.Length>0&&r.choices[0].delta!=null){
     string part=r.choices[0].delta.content;
     if(!string.IsNullOrEmpty(part)){
      if(first<0)first=Time.realtimeSinceStartup;
      currentReply+=part;log.text+=part;
     }
    }
   }catch{}
  }
 }
 void OnApplicationQuit(){if(apiKeyInput!=null)apiKeyInput.text="";}
 void OnDestroy(){if(apiKeyInput!=null)apiKeyInput.text="";}
 void LogResult(int npc,float total,float firstMs){
  string path=Path.Combine(Application.persistentDataPath,"douni-experiment.csv");bool exists=File.Exists(path);
  using(var sw=new StreamWriter(path,true,Encoding.UTF8)){if(!exists)sw.WriteLine("timestamp,npc,streaming,typing_indicator,first_token_ms,total_ms");sw.WriteLine(DateTime.Now.ToString("s")+","+names[npc]+","+streaming+","+typingIndicator+","+firstMs.ToString("F0")+","+(total*1000f).ToString("F0"));}
 }
}
public class CatEar : MonoBehaviour {
 public Transform left,right;bool talking=false;float phase=0;
 public void SetTalking(bool value){talking=value;if(!value){if(left)left.localRotation=Quaternion.Euler(0,0,-8f);if(right)right.localRotation=Quaternion.Euler(0,0,8f);}}
 void Update(){if(talking){phase+=Time.deltaTime*7f;if(left)left.localRotation=Quaternion.Euler(0,0,-8f+Mathf.Sin(phase)*4f);if(right)right.localRotation=Quaternion.Euler(0,0,8f-Mathf.Sin(phase)*4f);}}
}
