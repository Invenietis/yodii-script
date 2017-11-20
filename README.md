# yodii-script

Yodii.Script is a purely interpreted script language loosely based on javascript that supports function
with closures. One of its major specificty is to be implemnted as a state machine: it evaluates its
script in the calling thread, step-by-step or per time-slice without any cross threads concerns.
A very simple template engine (based on &lt;% ... %&gt; and &lt;%= ... %&gt; tags) is also available.

It is under developpement and any contributions are welcome.

## Key aspects
- Primary goal is full thread safety and API security. *Not performance!*
- Safe scripting language implemented as a a *state machine* (no thread at all but nevertheless interuptible: breakpoints, step in, step over, etc.).
- *Easy binding* (two-way for writable properties and fields for instance) to any external .Net object (uses reflection) that relies on a rather original way to publish an API to the script.
- *No dependency* (currently released only on .Net 4.5.1 and netstandard1.3) and *still less than 100KB* dll.
- Inspired from javascript but with important differences to be more *.Net compliant*.
- No more === and !==
  - Current == and != operators act as strict operators.
- With object support (the awful with: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/with). 
  - Because this "feature" can be useful to adapt existing DSL.

## To Do list
- Enable API securization (currently any properties or methods of external objects are callable).
  - A simple call validation hook should minimally do the job.
  - White/Black list and support of a kind of [SafeScript] attribute or ISafeScript marker interface may be useful.
- "Number" must be replaced with "Integer" and "Double". Integer must be the default but implicit conversion between 
  the two must be supported.
- Support a DateTime .Net object.
- String currently supports only indexer [] (instead of charAt() javascript method) and ToString().
  - StringObj must support all other useful methods (Contains, Substring, etc.).
- Export script functions as native functions (NativeFunctionObj does the job in the opposite way):
  - From the script to the external world (FunctionObj.ToNative() must return a callable delegate). 
    Evaluation of such delegate must take place on the primary thread on dedicated frame stacks.
- Transparently support async/await (actually any awaitable return) with the defined 
  but not implemented PExpr.DeferredKind.AsyncCall.

