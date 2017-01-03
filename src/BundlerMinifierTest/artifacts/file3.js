(function (factory) {
    factory();
}
(function () {
    "use strict";
    var global = (0, eval)('this');
    console.log(global); // Is undefined when run from the minified file
}));