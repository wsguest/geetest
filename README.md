# try to crack geetest in CSharp
## dependencies: ##
+ [ServiceStack](https://github.com/ServiceStack/ServiceStack/)

## usage: ##
<pre><code>

var gk = new Geek([gt], [site]);
//// or
//var gk = new Geek(new Uri([gtUrl]));
var jsonObj = gk.GetValidate();
if (jsonObj != null)
    // success
    // Console.WriteLine(jsonObj);
else
    // failed
</code></pre>

## demo: 
+ [online test](http://120.25.101.52/gee/test.aspx )
+ [demo](http://120.25.101.52/gee/geetest.gif)

## contact:  ##
+ QQ:1595152095 AT qq.com