# yodii-script
Safe scripting language implemented as a a state machine (no thread at all but nevertheless interuptible: breakpoints, step in, step over, etc.).

The goal of this project is to integrate a REALLY safe scripting language with a funny and little IDE in Civikey.

Based on CK-Javascript that was a pure expression evaluator, this one  is a sort ofvery light javascript that supports functions, closure, let to define variables, while/ do while, break, continue, if, and all the javascript operators (even the >>>= :)). (Yes for(;;) is missing for the moment.)
There is no "object" other than the primitive type (this is planned but without prototype).


It is under developpement and any contributions are welcome.
