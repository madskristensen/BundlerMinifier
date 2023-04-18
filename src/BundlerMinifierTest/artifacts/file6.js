function test(foo) {
    var bar = foo ?? 'bar';
    var text = `${foo ?? bar ?? 'text'}`;

    var func2 = () => `${foo ?? bar ?? 'text'}`;
}