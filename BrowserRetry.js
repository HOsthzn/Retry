const Retry = {
    Strategy: {
        FIXED_INTERVAL: "FixedInterval",
        EXPONENTIAL_BACKOFF: "ExponentialBackOff",
        JITTER: "Jitter",
    },

    getRetryInterval: (attempt, retryInterval, strategy, maxDelay = Infinity) => {
        if (retryInterval <= 0) throw new Error("retryInterval must be positive");
        let interval;
        switch (strategy) {
            case Retry.Strategy.EXPONENTIAL_BACKOFF:
                interval = retryInterval * Math.pow(2, attempt);
                break;
            case Retry.Strategy.JITTER:
                interval = retryInterval * Math.pow(2, attempt) * (Math.random() * 0.4 + 0.8); // ±20% jitter
                break;
            default: // FIXED_INTERVAL
                interval = retryInterval;
        }
        return Math.min(interval, maxDelay);
    },

    attemptAsync: async (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL, logger = console.log, maxDelay = Infinity) => {
        if (maxAttempts <= 0) throw new Error("maxAttempts must be positive");
        if (typeof action !== 'function') throw new Error("action must be a function");

        const exceptions = [];
        for (let attempt = 0; attempt < maxAttempts; attempt++) {
            try {
                if (attempt > 0) {
                    const delay = Retry.getRetryInterval(attempt, retryInterval, strategy, maxDelay);
                    await new Promise(resolve => setTimeout(resolve, delay));
                    logger(`Attempt ${attempt + 1} failed, retrying in ${delay}ms`);
                }
                return await action();
            } catch (ex) {
                exceptions.push(ex);
                if (attempt === maxAttempts - 1) {
                    logger('All attempts failed. Exceptions:', exceptions);
                    throw new Error(`All attempts failed after ${maxAttempts} retries: ${exceptions.map(e => e.message).join(', ')}`);
                }
            }
        }
    }
};
