
# Delta Programming Language

Delta is a **high-level**, statically typed programming language designed to be simple yet powerful. It combines **object-oriented** and **procedural** programming paradigms with easy-to-read syntax, making it ideal for intermediate developers who want to focus on logic rather than language quirks.

---

## ✨ Features

- Clean, beginner-friendly syntax
- Statically typed for safety and clarity
- Supports both **object-oriented** and **procedural** programming styles
- REPL-based execution (for now)
- Designed with future support for compiled execution

> ⚠️ Note: Delta is still in development. The standard library and file-based execution are planned for future releases.

---

## Syntax Overview

### Expressions

---

#### 🔹 `LiteralExpr`

**Definition:**  
`<string|number|bool>`

**Examples:**
```delta
42
"hello"
true
```

---

#### 🔹 `UnaryExpr`

**Definition:**  
`<operator> <expr>`

**Examples:**
```delta
!true
-5
```

---

#### 🔹 `BinaryExpr`

**Definition:**  
`<expr> <operator> <expr>`

**Examples:**
```delta
a + b
5 * 3
x != y
```

---

#### 🔹 `GroupingExpr`

**Definition:**  
`( <expr> )`

**Examples:**
```delta
(1 + 2) * 3
```

---

#### 🔹 `NameExpr`

**Definition:**  
`<name>`

**Examples:**
```delta
x
result
```

---

#### 🔹 `AssignExpr`

**Definition:**  
`<name> = <expr>`

**Examples:**
```delta
x = 10
message = "Delta"
```

---

#### 🔹 `GetExpr`

**Definition:**  
`<expr>.<propName>`

**Examples:**
```delta
obj.value
user.name
```

---

#### 🔹 `SetExpr`

**Definition:**  
`<expr>.<propName> = <expr>`

**Examples:**
```delta
obj.value = 42
obj.name = "Delta"
```

---

#### 🔹 `CallExpr`

**Definition:**  
`<name>( ...args )`

**Examples:**
```delta
print("Hello")
sum(1, 2)
```

---

#### 🔹 `MethodExpr`

**Definition:**  
`<expr>.<methodName>( ...args )`

**Examples:**
```delta
user.login("admin")
point.move(1, 2)
```

---

### 📙 Statements

---

#### 🔸 `ExprStmt`

**Definition:**  
`<expr>[;]`

**Examples:**
```delta
print("Hello")
x + y;
```

---

#### 🔸 `VarStmt`

**Definition:**  
`var [mutToken] name [:typeClause] = <expr>[;]`

**Examples:**
```delta
var x = 5
var mut y: int = 3;
```

---

#### 🔸 `BlockStmt`

**Definition:**  
`{ ...stmts }`

**Examples:**
```delta
{
    var x = 1
    print(x)
}
```

---

#### 🔸 `IfStmt`

**Definition:**  
`if <expr> <stmt> [else <stmt>]`

**Examples:**
```delta
if x > 0 {
    print("positive")
} else
    print("negative")
```

---

#### 🔸 `LoopStmt`

**Definition:**  
`loop [expr] <stmt>`

**Example:**
```delta
loop {
    print("forever")
}
```

---

#### 🔸 `ForStmt`

**Definition:**  
`for <name> = <expr> -> <expr> [step <expr>] <stmt>`

**Examples:**
```delta
for i = 0 -> 5 {
    print(i)
}

for j = 1 -> 10 step 2 {
    print(j)
}
```

---

#### 🔸 `RetStmt`

**Definition:**  
`ret [<expr>];`

**Examples:**
```delta
ret;
ret 42;
```

---

#### 🔸 `BreakStmt`

**Definition:**  
`break;`

---

#### 🔸 `ContinueStmt`

**Definition:**  
`continue;`

---

#### 🔸 `FnDecl`

**Definition:**  
`fn <name>([...parameters]) -> <type> <stmt>`

**Examples:**
```delta
fn add(a: int, b: int) -> int {
    ret a + b;
}
```

---

#### 🔸 `ClassDecl`

**Definition:**  
`class name { [...properties] [...methods] [constructor] }`

**Examples:**
```delta
class Point {
    mut x = 0;
    mut y = 0;

    fn(x1: int, y1: int) {
        x = x1
        y = y1
    }

    fn move(dx: int, dy: int) -> void {
        x = x + dx
        y = y + dy
    }
}
```

---

## Getting Started

### 1. Running Delta

Currently, Delta can be executed in REPL mode:

1. Open the project in Visual Studio.
2. Start the application.
3. Enter your Delta code directly into the REPL prompt.

*File execution is coming soon!*

---

### 2. Hello World

```delta
print("Hello, World!")
```

---

## 📁 File Extensions

Delta currently does not use a specific file extension, but support for file-based scripts is planned.

---

## 🔓 License

Delta is released under the [MIT License](https://opensource.org/licenses/MIT).

---

## 🛠️ Contributing

We welcome contributions! Here's how to get started:

1. **Fork** the repository  
2. **Clone** your fork:  
   ```bash
   git clone https://github.com/your-username/Delta.git
   ```
3. **Create a new branch** for your feature or bugfix:  
   ```bash
   git checkout -b my-feature
   ```
4. Make your changes and commit them with clear messages  
5. **Push** to your fork and create a **Pull Request** on the main repo

Please make sure your code follows the existing style and structure. If you're unsure where to begin, check the open issues or ask a question in the repository.

---

## 📍 Repository

[GitHub – Stonygeist123/Delta](https://github.com/Stonygeist123/Delta/tree/master)

---

*Delta is a project in progress — stay tuned for updates and new features!*
