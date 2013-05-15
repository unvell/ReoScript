ReoScript
=========
**Script language interpreter for .Net programs**

ReoScript is a powerful JavaScript-like script language engine implemented in C#. It was be designed for inclusion in applications that require a built-in, easy to use, scalable script language with no dependencies other language like C/C++.

## Features

- Fully JavaScript statements and operators implement
- JSON and Array literals
- Fully prototype chain mechanism
- Lambda Expression
- Enhanced Array Enumerator and Collection Operations
- Console Runner
- Data exchange with .Net CLR
- Property Extension and Function Extension
- Directly access .Net object with DirectAccess
- Import and using .Net Types in script
- Auto CLR Event Binding

ReoScript is not completely compliant to JavaScript/ECMAScript standard, it has stricter syntax check and own additional syntax, function and object in order to enhance usability of data exchange. 

ReoScript provides a simple script Editor it can be used to write and execute script directly. The Editor can also be included in application and provided to end-user.

## Hello World!

1. Download or build the following DLLs, add references to target project

        Antlr3.Runtime.dll
        Unvell.ReoScript.dll

2. Import the following namespace
    
        using Unvell.ReoScript;

3. Create ScriptRunningMachine and run script
    
        ScriptRunningMachine srm = new ScriptRunningMachine();
        srm.Run("alert('hello world!');");

    (semicolons at end of line in ReoScript are required)

## Development Documents

   https://github.com/unvell/ReoScript/wiki

## Offical Website
   Find more Sample programs and Documents at:
   http://www.unvell.com/ReoScript

   Language Specification see the test-cases:
   https://github.com/unvell/ReoScript/tree/master/TestCase/tests
   
## Change Log

   https://github.com/unvell/ReoScript/wiki/Change-Log
   
## Third-Party

The following softwares may be included in this product:

- ANTLR v3
- FastColoredTextBox

## License

ReoScript and ReoScript Editor released under GNU Lesser General Public License (LGPLv3).

Jing, Lu (lujing@unvell.com)

copyright (c) unvell.com 2012-2013 all rights reserved.
