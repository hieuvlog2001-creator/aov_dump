
let activeWorker = null;

window.backgroundTask = {
    start: function (dotnet) {
        this.cancel();

        const worker = new Worker(
            new URL("background.js", document.baseURI)
        );

        activeWorker = worker;

        worker.onmessage = async function (event) {
            if (activeWorker !== worker) return;

            const { type, value } = event.data;

            try {
                await dotnet.invokeMethodAsync(
                    "UpdateWorker",
                    type,
                    String(value)
                );
            } finally {
                if (type === "complete" || type === "error") {
                    worker.terminate();

                    if (activeWorker === worker) {
                        activeWorker = null;
                    }
                }
            }
        };

        worker.onerror = function (event) {
            if (activeWorker !== worker) return;

            worker.terminate();
            activeWorker = null;

            dotnet.invokeMethodAsync(
                "UpdateWorker",
                "error",
                event.message
            );
        };

        worker.postMessage({ start: true });
    },

    cancel: function () {
        if (activeWorker) {
            activeWorker.terminate();
            activeWorker = null;
        }
    }
};
