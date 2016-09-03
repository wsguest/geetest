# Try to crack geetest in C\#
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

## demo: ##
+ Last night someone attacked the demo server, resulting in DoS, and now the demo link can not be opened for exhausted traffic. In the future, I will publish new behavior algorithm.(9/3/16)
+ ~~[online test](http://experiment.imwork.net/gee/test.aspx)~~
+ [demo.gif](/demo/geetest.gif)

## contact:  ##
+ 1595152095 AT qq.com