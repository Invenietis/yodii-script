# yodii-script

Yodii.Script is a purely interpreted script language loosely based on javascript that supports function
with closures. One of its major specificty is to be implemnted as a state machine: it evaluates its
script in the calling thread, step-by-step or per time-slice without any cross threads concerns.
A very simple template engine (based on &lt;% ... %&gt; and &lt;%= ... %&gt; tags) is also available.

It is under developpement and any contributions are welcome.

## Key aspects
- Primary goal is full thread safety and API security. Not performance!
- Safe scripting language implemented as a a state machine (no thread at all but nevertheless interuptible: breakpoints, step in, step over, etc.).
- Easy binding (two-way for writable properties and fields) to any external .Net object (uses reflection) but also an original way to publish an API to the script.
- No dependency (currently released only on .Net 4.5.1) and less than 100KB dll.
- Inspired from javascript but with important differences.

## To Do list
- Enable API securization (currently any properties or methods of external objects are callable).
  - A simple call validation hook should minimally do the job.
  - White/Black list and support of a kind of [SafeScript] attribute may be useful.
- Stop supporting javascript operators === and !==
  - Current == and != operators must simply use .Net object.Equals method: no implicit conversion must be made.
- "Number" must be replaced with "Integer" and "Double". Integer must be the default but implicit conversion between 
  the two must be supported.
- String currently supports only indexer [] (instead of charAt() javascript method) and ToString() :).
  - StringObj must support all other useful methods (Contains, Substring, etc.).
- Two-way support for native functions:
  - From the external world to the script (any delegate must be callable just like objects' methods).
  - From the script to the external world (FunctionObj.ToNative() must return a callable delegate). 
    Evaluation of such delegate must take place on the primary thread on dedicated frame stacks.
