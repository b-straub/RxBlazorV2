## Release 1.0

### New Rules

| Rule ID | Category | Severity | Notes |
|---------|----------|----------|-------|
| RXBG001 | RxBlazorGenerator | Error | Observable model analysis error |
| RXBG002 | RxBlazorGenerator | Error | Code generation error |
| RXBG003 | RxBlazorGenerator | Warning | Method analysis warning |
| RXBG010 | RxBlazorGenerator | Error | Circular model reference detected |
| RXBG011 | RxBlazorGenerator | Error | Invalid model reference target |
| RXBG012 | RxBlazorGenerator | Error | Referenced model has no used properties |
| RXBG013 | RxBlazorGenerator | Error | Cannot reference derived ObservableModel |
| RXBG014 | RxBlazorGenerator | Error | ObservableModel used by multiple components must have Singleton scope |
| RXBG020 | RxBlazorGenerator | Error | Generic type arity mismatch |
| RXBG021 | RxBlazorGenerator | Error | Type constraint mismatch |
| RXBG022 | RxBlazorGenerator | Error | Invalid open generic type reference |
| RXBG030 | RxBlazorGenerator | Error | Command trigger type arguments mismatch |
| RXBG031 | RxBlazorGenerator | Error | Circular trigger reference detected |
| RXBG032 | RxBlazorGenerator | Error | Command execute method should not return a value |
| RXBG033 | RxBlazorGenerator | Error | Command execute method must return a value |
| RXBG040 | RxBlazorGenerator | Error | Invalid init accessor on partial property |
| RXBG041 | RxBlazorGenerator | Warning | ObservableComponentTrigger attribute has no effect |
| RXBG050 | RxBlazorGenerator | Info | Partial constructor parameter type may not be registered in DI |
| RXBG051 | RxBlazorGenerator | Error | DI service scope violation |
| RXBG060 | RxBlazorGenerator | Error | Direct inheritance from ObservableComponent is not supported |
| RXBG061 | RxBlazorGenerator | Error | Generated component used for composition in same assembly without @page directive |
| RXBG062 | RxBlazorGenerator | Error | Component has no reactive properties or triggers |
| RXBG070 | RxBlazorGenerator | Warning | ObservableModel is missing ObservableModelScope attribute |
| RXBG071 | RxBlazorGenerator | Error | Partial constructor with DI parameters must be public |
| RXBG072 | RxBlazorGenerator | Error | Observable entity must be declared as partial |
| RXBG080 | RxBlazorGenerator | Error | ObservableModelObserver method has invalid signature |
| RXBG081 | RxBlazorGenerator | Error | ObservableModelObserver property not found on model |
| RXBG082 | RxBlazorGenerator | Warning | Internal model observer method has invalid signature |
| RXBG090 | RxBlazorGenerator | Warning | Direct access to Observable property |
