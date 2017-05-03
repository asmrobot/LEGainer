# LEGainer
通过Let's Encrypt 获取证书工具<br/>
<br/>
<br/>
<br/>
每月申请，安装为windows计划任务：<br/>
schtasks /create /tn "letsencrypt_https" /tr path\to\LEGainer.exe /sc monthly   /ru System<br/>
<br/>
<br/>
<br/>
//时间短点测试可用：<br/>
schtasks /create /tn "letsencrypt_https" /tr path\to\LEGainer.exe /sc minute /mo 3   /ru System<br/>

