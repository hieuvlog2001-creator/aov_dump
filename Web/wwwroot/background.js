
self.onmessage = function (event) {
    try {
        const total = 100000000;
        let result = 0;

        for (let i = 0; i < total; i++) {
            result += Math.sqrt(i);

            if (i % 1000000 === 0) {
                self.postMessage({
                    type: "progress",
                    value: Math.floor(i / total * 100)
                });
            }
        }

        self.postMessage({
            type: "complete",
            value: result
        });
    } catch (error) {
        self.postMessage({
            type: "error",
            value: error.message
        });
    }
};
