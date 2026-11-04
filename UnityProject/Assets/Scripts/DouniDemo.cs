using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.EventSystems;

[Serializable] public class DouniMsg { public string role; public string content; public DouniMsg(string r,string c){role=r;content=c;} }
[Serializable] class DouniReq { public DouniMsg[] messages; public bool stream; }
[Serializable] class DouniDelta { public string content; }
[Serializable] class DouniChoice { public DouniDelta delta; public DouniMsg message; }
[Serializable] class DouniResp { public DouniChoice[] choices; }
[Serializable] class DouniMemoryData { public List<DouniMsg> red=new List<DouniMsg>(); public List<DouniMsg> blue=new List<DouniMsg>(); }

public class DouniDemo : MonoBehaviour {
 [Header("实验条件")] public bool streaming=true; public bool typingIndicator=true;
 public string proxyUrl="http://127.0.0.1:3000/chat";
 Text log, state; InputField entry, apiKeyInput; ScrollRect chatScroll; RectTransform chatContent; Text liveBubble; string pendingStreamText=""; bool scrollPending=false,followOutput=true; ScrollRect chatScroll; RectTransform chatContent; string pendingStreamText=""; bool scrollPending=false; Button sendBtn, clearChatBtn, streamModeBtn, typingModeBtn, memoryModeBtn, memoryViewBtn, exportBtn; GameObject memoryPanel; int active=0; bool busy=false;
 readonly List<DouniMsg>[] history={new List<DouniMsg>(),new List<DouniMsg>()};
 readonly string[] names={"Red Scarf Douni","Blue Scarf Douni"};
 readonly string[] prompts={
 "你是Red Scarf Douni，活泼、热情、爱鼓励人，中文回答自然可爱，尽量简短。",
 "你是Blue Scarf Douni，冷静、温柔、耐心，善于解释问题，中文回答清楚简洁。"
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
  if(FindObjectOfType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
  if(FindObjectOfType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
  var c=new GameObject("DouniCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
  c.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;c.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1440,900);
  var panel=Rect(c.transform,"ChatPanel",new Vector2(0,0),new Vector2(700,690),new Color(1,.96f,.9f,.97f));
  Button red=Btn(c.transform,"Red Scarf Douni",new Vector2(-170,300),new Vector2(250,58),()=>Select(0));
  Button blue=Btn(c.transform,"Blue Scarf Douni",new Vector2(170,300),new Vector2(250,58),()=>Select(1));
  apiKeyInput=Input(c.transform,new Vector2(0,235),new Vector2(560,42));apiKeyInput.inputType=InputField.InputType.Password;apiKeyInput.placeholder=Txt(apiKeyInput.transform,"Enter DeepSeek API Key (session only)",Vector2.zero,new Vector2(540,40),16,TextAnchor.MiddleLeft);
  BuildChatScroll(c.transform);
  streamModeBtn=Btn(c.transform,"Streaming: ON",new Vector2(-155,-250),new Vector2(220,42),ToggleStream);
  typingModeBtn=Btn(c.transform,"Typing Indicator: ON",new Vector2(155,-250),new Vector2(220,42),ToggleTyping);
  memoryModeBtn=Btn(c.transform,"Local Memory: ON",new Vector2(-180,-205),new Vector2(180,38),ToggleMemory);
  memoryViewBtn=Btn(c.transform,"View Memory",new Vector2(20,-205),new Vector2(150,38),ToggleMemoryPanel);
  exportBtn=Btn(c.transform,"Export CSV",new Vector2(200,-205),new Vector2(150,38),ExportCsv);
  state=Txt(c.transform,"Waiting for local proxy",new Vector2(0,-285),new Vector2(620,26),15,TextAnchor.MiddleCenter);
  entry=Input(c.transform,new Vector2(-70,-320),new Vector2(470,58));
  sendBtn=Btn(c.transform,"Send",new Vector2(255,-320),new Vector2(120,58),Send);
  clearChatBtn=Btn(c.transform,"Clear Chat",new Vector2(0,-365),new Vector2(160,42),ClearCurrentChat);
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
 void BuildChatScroll(Transform parent){
  var vg=new GameObject("ChatViewport",typeof(RectTransform),typeof(Image),typeof(Mask));vg.transform.SetParent(parent,false);
  var vr=vg.GetComponent<RectTransform>();vr.anchorMin=vr.anchorMax=new Vector2(.5f,.5f);vr.anchoredPosition=new Vector2(0,5);vr.sizeDelta=new Vector2(620,450);vg.GetComponent<Image>().color=new Color(1f,.985f,.96f,.2f);vg.GetComponent<Mask>().showMaskGraphic=false;
  var cg=new GameObject("ChatContent",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));cg.transform.SetParent(vg.transform,false);
  chatContent=cg.GetComponent<RectTransform>();chatContent.anchorMin=new Vector2(0,1);chatContent.anchorMax=new Vector2(1,1);chatContent.pivot=new Vector2(.5f,1);chatContent.sizeDelta=Vector2.zero;
  var group=cg.GetComponent<VerticalLayoutGroup>();group.childControlHeight=true;group.childForceExpandHeight=false;group.childControlWidth=true;group.childForceExpandWidth=true;group.spacing=10;group.padding=new RectOffset(12,12,12,12);cg.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  chatScroll=vg.AddComponent<ScrollRect>();chatScroll.viewport=vr;chatScroll.content=chatContent;chatScroll.horizontal=false;chatScroll.vertical=true;chatScroll.movementType=ScrollRect.MovementType.Clamped;chatScroll.inertia=true;chatScroll.decelerationRate=.12f;chatScroll.scrollSensitivity=28;
  chatScroll.onValueChanged.AddListener(v=>{if(chatScroll.verticalNormalizedPosition>.08f)followOutput=false;else followOutput=true;});
 }
 void AddBubble(string message,bool fromUser,bool live){
  var row=new GameObject("MessageRow",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(ContentSizeFitter));row.transform.SetParent(chatContent,false);
  var rowRt=row.GetComponent<RectTransform>();rowRt.sizeDelta=new Vector2(590,40);
  var rowLayout=row.GetComponent<HorizontalLayoutGroup>();rowLayout.childAlignment=fromUser?TextAnchor.MiddleRight:TextAnchor.MiddleLeft;rowLayout.childControlHeight=true;rowLayout.childForceExpandHeight=false;rowLayout.childControlWidth=false;rowLayout.childForceExpandWidth=false;
  row.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  var bubble=new GameObject("Bubble",typeof(RectTransform),typeof(Image),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));bubble.transform.SetParent(row.transform,false);
  bubble.GetComponent<Image>().color=fromUser?new Color(0.97f,.84f,.77f):new Color(1f,1f,1f,.94f);
  var bg=bubble.GetComponent<VerticalLayoutGroup>();bg.padding=new RectOffset(16,16,10,10);bg.childControlHeight=true;bg.childForceExpandHeight=false;bg.childControlWidth=true;bg.childForceExpandWidth=true;
  var fit=bubble.GetComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  var le=bubble.AddComponent<LayoutElement>();le.preferredWidth=fromUser?390:450;le.flexibleWidth=0;
  var tx=Txt(bubble.transform,message,Vector2.zero,new Vector2(fromUser?358:418,32),18,fromUser?TextAnchor.MiddleRight:TextAnchor.MiddleLeft);tx.horizontalOverflow=HorizontalWrapMode.Wrap;tx.verticalOverflow=VerticalWrapMode.Overflow;tx.supportRichText=false;
  var txFit=tx.gameObject.AddComponent<ContentSizeFitter>();txFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  var txRt=tx.rectTransform;txRt.sizeDelta=new Vector2(fromUser?358:418,32);
  if(live)liveBubble=tx;
 }
 void LateUpdate(){
  if(!string.IsNullOrEmpty(pendingStreamText)&&liveBubble!=null){liveBubble.text+=pendingStreamText;pendingStreamText="";scrollPending=true;}
  if(scrollPending&&chatScroll!=null){Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);if(followOutput)chatScroll.verticalNormalizedPosition=0f;scrollPending=false;}
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
 void Refresh(){
  foreach(Transform child in chatContent)Destroy(child.gameObject);
  liveBubble=null;
  for(int i=0;i<history[active].Count;i++){var m=history[active][i];if(m.role=="system")continue;AddBubble(m.content,m.role=="user",false);}
  if(history[active].Count<=1)AddBubble("Hello! I am "+names[active]+". What would you like to talk about?",false,false);
  scrollPending=true;followOutput=true;
 }
 void ToggleStream(){if(busy)return;streaming=!streaming;streamModeBtn.GetComponentInChildren<Text>().text="流式输出："+(streaming?"开":"关");}
 void ToggleTyping(){if(busy)return;typingIndicator=!typingIndicator;typingModeBtn.GetComponentInChildren<Text>().text="输入提示："+(typingIndicator?"开":"关");}
 void ClearCurrentChat(){if(busy)return;history[active].Clear();history[active].Add(new DouniMsg("system",prompts[active]));SaveMemory();Refresh();state.text="Current character history cleared";}
 void ToggleMemory(){if(busy)return;localMemoryEnabled=!localMemoryEnabled;memoryModeBtn.GetComponentInChildren<Text>().text="本地记忆："+(localMemoryEnabled?"开":"关");if(!localMemoryEnabled){DeleteMemoryFile();history[0].Clear();history[1].Clear();EnsureSystemPrompts();Refresh();state.text="本地记忆已Close并清除";}else{SaveMemory();state.text="Local memory enabled (this device only)";}}
 void ToggleMemoryPanel(){if(busy)return;if(memoryPanel!=null){Destroy(memoryPanel);memoryPanel=null;return;}memoryPanel=Rect(GameObject.Find("DouniCanvas").transform,"MemoryPanel",Vector2.zero,new Vector2(600,470),new Color(1f,.96f,.9f,.99f));var text=Txt(memoryPanel.transform,"Local memory (stored on this device)\n\n"+BuildMemoryText(active),Vector2.zero,new Vector2(540,400),17,TextAnchor.UpperLeft);Btn(memoryPanel.transform,"Close",new Vector2(0,-210),new Vector2(140,42),()=>{Destroy(memoryPanel);memoryPanel=null;});}
 string BuildMemoryText(int npc){var b=new StringBuilder();for(int i=1;i<history[npc].Count;i++)b.Append(history[npc][i].role=="user"?"User: ":"Douni: ").Append(history[npc][i].content).Append("\n\n");return b.ToString();}
 void ExportCsv(){string path=Path.Combine(Application.persistentDataPath,"douni-experiment.csv");if(!File.Exists(path)){state.text="No experiment data to export";return;}GUIUtility.systemCopyBuffer=path;state.text="CSV path copied to clipboard";}
 void EnsureSystemPrompts(){for(int i=0;i<2;i++){if(history[i].Count==0||history[i][0].role!="system")history[i].Insert(0,new DouniMsg("system",prompts[i]));}}
 void LoadMemory(){if(!File.Exists(memoryPath))return;try{var data=JsonUtility.FromJson<DouniMemoryData>(File.ReadAllText(memoryPath,Encoding.UTF8));if(data!=null){history[0]=data.red??new List<DouniMsg>();history[1]=data.blue??new List<DouniMsg>();}}catch{history[0].Clear();history[1].Clear();}}
 void TrimHistory(int npc){while(history[npc].Count>81)history[npc].RemoveAt(1);}
 void SaveMemory(){if(!localMemoryEnabled)return;try{var data=new DouniMemoryData{red=history[0],blue=history[1]};File.WriteAllText(memoryPath,JsonUtility.ToJson(data,true),Encoding.UTF8);}catch(Exception e){Debug.LogWarning("Douni local memory save failed: "+e.Message);}}
 void DeleteMemoryFile(){try{if(File.Exists(memoryPath))File.Delete(memoryPath);}catch(Exception e){Debug.LogWarning("Douni memory delete failed: "+e.Message);}}
 void Send(){if(busy||string.IsNullOrWhiteSpace(entry.text))return;string q=entry.text.Trim();entry.text="";history[active].Add(new DouniMsg("user",q));TrimHistory(active);SaveMemory();Refresh();StartCoroutine(RequestReply(active));}
 IEnumerator RequestReply(int npc){
  busy=true;started=Time.realtimeSinceStartup;first=-1;currentReply="";pendingStreamText="";liveBubble=null;redEar.SetTalking(npc==0);blueEar.SetTalking(npc==1);sendBtn.interactable=false;
  if(typingIndicator)state.text=names[npc]+" is typing…";else state.text=" ";
  string apiKey=apiKeyInput.text.Trim();
  if(string.IsNullOrEmpty(apiKey)){state.text="Please enter your DeepSeek API Key.";busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}
  // App-managed memory: send the recent local chat context explicitly; no provider memory is used.
  var context=new List<DouniMsg>();context.Add(new DouniMsg("system",prompts[npc]));
  int firstContext=Math.Max(1,history[npc].Count-20);
  for(int i=firstContext;i<history[npc].Count;i++)context.Add(history[npc][i]);
  var reqBody=JsonUtility.ToJson(new DouniReq{messages=context.ToArray(),stream=streaming});
  using(var req=new UnityWebRequest(proxyUrl,"POST")){
   req.timeout=90;req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(reqBody));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");req.SetRequestHeader("x-deepseek-api-key",apiKey);
   var op=req.SendWebRequest();int read=0;
   while(!op.isDone){
    if(streaming){string all=req.downloadHandler.text;if(all.Length>read){string fresh=all.Substring(read);read=all.Length;ParseSSE(fresh);}}
    yield return null;
   }
   if(req.result!=UnityWebRequest.Result.Success){state.text="Request failed: "+req.error+" (check proxy, network, API key and quota).";busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}
   if(streaming){if(liveBubble==null)AddBubble("",false,true);ParseSSE(req.downloadHandler.text.Substring(Mathf.Min(read,req.downloadHandler.text.Length)));if(string.IsNullOrEmpty(currentReply)){state.text="No response received. Please try again.";busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}}
   else{try{var response=JsonUtility.FromJson<DouniResp>(req.downloadHandler.text);if(response==null||response.choices==null||response.choices.Length==0||response.choices[0].message==null||string.IsNullOrEmpty(response.choices[0].message.content))throw new Exception("empty response");currentReply=response.choices[0].message.content;}catch{state.text="Invalid response format. Please try again.";busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);yield break;}}
  }
  if(first<0)first=Time.realtimeSinceStartup;history[npc].Add(new DouniMsg("assistant",currentReply));TrimHistory(npc);SaveMemory();
  Refresh();
  LogResult(npc,Time.realtimeSinceStartup-started,(first-started)*1000f);
  busy=false;sendBtn.interactable=true;redEar.SetTalking(false);blueEar.SetTalking(false);state.text="Done · First token: "+((first-started)*1000f).ToString("F0")+" ms";
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
      if(liveBubble==null)AddBubble("",false,true);currentReply+=part;pendingStreamText+=part;
     }
    }
   }catch{}
  }
 }
 void OnApplicationQuit(){if(apiKeyInput!=null)apiKeyInput.text="";}
 void OnDestroy(){if(apiKeyInput!=null)apiKeyInput.text="";}
 void LogResult(int npc,float total,float firstMs){
  try{
   string path=Path.Combine(Application.persistentDataPath,"douni-experiment.csv");bool exists=File.Exists(path);
   using(var sw=new StreamWriter(path,true,Encoding.UTF8)){if(!exists)sw.WriteLine("timestamp,npc,streaming,typing_indicator,first_token_ms,total_ms");sw.WriteLine(DateTime.Now.ToString("s")+","+names[npc]+","+streaming+","+typingIndicator+","+firstMs.ToString("F0")+","+(total*1000f).ToString("F0"));}
  }catch(Exception e){state.text="回复成功，但实验数据保存失败："+e.Message;}
 }
}
public class CatEar : MonoBehaviour {
 public Transform left,right;bool talking=false;float phase=0;
 public void SetTalking(bool value){talking=value;if(!value){if(left)left.localRotation=Quaternion.Euler(0,0,-8f);if(right)right.localRotation=Quaternion.Euler(0,0,8f);}}
 void Update(){if(talking){phase+=Time.deltaTime*7f;if(left)left.localRotation=Quaternion.Euler(0,0,-8f+Mathf.Sin(phase)*4f);if(right)right.localRotation=Quaternion.Euler(0,0,8f-Mathf.Sin(phase)*4f);}}
}
