Making-extension-method-debuging-easier.md

While searching materials for "How to start writing functional code in C#" I found this article where Dave Fancher writes about debugging method chains. I didn't quite get it after first reading what System.Diagnostics.DebuggerNonUserCodeAttribute is doing here, so I decided to try it out by myself.

I've created small .NET Core Console app printing text in three different ways, using extension methods. Here is GIF I recorded during debugging the code.

In the GIF you can see me stepping into every method in the chain. Notice that some of the methods are omitted by debugger, but all of them are actualy invoked (all three lines are printed in console).

Points taken

- The DebuggerNonUserCode attribute marks 
