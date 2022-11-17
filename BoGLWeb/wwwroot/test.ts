namespace test {
    class Test {
        public test(text: string) {
            console.log(text);
        }
    }

    export function getTest() : Test {
        return new Test();
    }
}