# try to crack geetest in CSharp
## dependencies: ##
+ [ServiceStack.Text](https://github.com/ServiceStack/ServiceStack.Text) - [install](https://www.nuget.org/packages/ServiceStack.Text/)

## usage: ##
<pre><code>

            var gk = new Geek([gt], [site]);
            var jsonObj = gk.GetValidate();
            if (jsonObj != null)
                Console.WriteLine(jsonObj["validate"]);
            else
                Console.WriteLine("*** failed ***");
</code></pre>

## demo: 
+ [online test](http://120.25.101.52/gee/test.aspx )
+ [demo](http://120.25.101.52/gee/geetest.gif)

## contact:  ##
+ QQ:1595152095 AT qq.com