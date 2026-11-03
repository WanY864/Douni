import express from "express";
import cors from "cors";
import dotenv from "dotenv";
dotenv.config({ path: new URL("./.env", import.meta.url) });
const app=express(), port=Number(process.env.PORT||3000);
app.use(cors()); app.use(express.json({limit:"32kb"}));
app.get("/health",(_q,s)=>s.json({ok:true,configured:!!process.env.DEEPSEEK_API_KEY}));
app.post("/chat",async(req,res)=>{
 const key=process.env.DEEPSEEK_API_KEY;
 if(!key)return res.status(503).json({error:"请在 proxy/.env 配置 DEEPSEEK_API_KEY"});
 const {messages,stream=true}=req.body||{};
 if(!Array.isArray(messages)||!messages.length||messages.length>30)return res.status(400).json({error:"messages 参数无效"});
 let upstream;
 try{upstream=await fetch("https://api.deepseek.com/chat/completions",{method:"POST",headers:{Authorization:"Bearer "+key,"Content-Type":"application/json"},body:JSON.stringify({model:process.env.DEEPSEEK_MODEL||"deepseek-chat",messages,stream:!!stream,temperature:.7})});}
 catch(e){return res.status(502).json({error:e.message});}
 if(!upstream.ok)return res.status(upstream.status).send(await upstream.text());
 if(!stream)return res.type("application/json").send(await upstream.text());
 res.set({"Content-Type":"text/event-stream; charset=utf-8","Cache-Control":"no-cache","Connection":"keep-alive"});
 const reader=upstream.body.getReader();
 try{while(true){const {value,done}=await reader.read();if(done)break;res.write(value);}}catch{}finally{res.end();}
});
app.listen(port,"127.0.0.1",()=>console.log("Douni proxy: http://127.0.0.1:"+port));
