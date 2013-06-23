ReoScript
=========
**Script language engine for .Net programs**

ReoScript is a powerful JavaScript-like script language engine implemented in C#. It was designed for inclusion in applications that require a built-in, easy to use, scalable script language with no dependencies other language like C/C++.

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

ReoScript is not completely compliant to JavaScript/ECMAScript standard, it has stricter syntax check and own additional syntax, functions and objects in order to enhance usability of data exchange. 

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


## Language Feature

### More simpler inheriting based on prototype mechanism 

**This feature is planned to implement in next release**.

    function A() {
      this.a = 10;
    }

    function B() : A() {
      this.b = 20;
    }

    var b = new B;
    console.log(b.a + b.a);

The output is:
   
    30

### Lambda Expression and Enhanced Collection Operations

Lambda based where, sum, min, max and etc. are available in Array operations.

    var arr = [1, 5, 7, 8, 10, 13, 20, 24, 26];
    var result = arr.where(n => n < 10).sum();

The result is:

    21

See [[Lambda Expression]].

### setTimeout and setInterval supported to pass arguments

    function check_alive(user) {
        if (...) {
            return 'ok';
        } else {
            setTimeout(check_alive, 1000, user);
        }
    }

    var user = getCurrentUser();
    setTimeout(check_alive, 1000, user);

See [[Enhanced Async-Calling]].

### Import script file into current script context

common.rs:

    function check_login(usr, pwd) {
        return (usr.password == hashed(pwd));
    }

login.rs:

    import "common.rs";

    var usr = getCurrentUser();
    check_login(usr, getInputtedPwd());

See [[import keyword]].

### Using CLR

The classes in .NET Common Language Runtime are available to script by using [[DirectAccess]].

### Initailizer

Initializer used to simplify the code by merging construction and property setting:

    var newApple = new Apple() {
        color : 'red',
    };
        
It is same as:

    var newApple = new Apple();
    newApple.color = 'red';

See [[Constructor and Initializer]].

### Binary literal supported (e.g. 0b1010)

    var num = 0b1010;
    console.log(num);

The output is:

    10

## Development Documents

   Development Documents are available at:
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
