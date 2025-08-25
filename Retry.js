// The Retry object contains various strategies for implementing retries
const Retry = {
    // Strategy object for different retry back-off mechanisms
    Strategy: {
        // Each retry waits for a fixed amount of time
        FIXED_INTERVAL: "FixedInterval",
        // The wait time for the next retry doubles after each attempt
        EXPONENTIAL_BACKOFF: "ExponentialBackOff",
        // The wait time is randomly selected for each retry
        RANDOM: "Random",
        // The wait time increases incrementally after each attempt
        INCREMENTAL: "Incremental",
        // The wait time follows the Fibonacci sequence after each attempt
        FIBONACCI: "Fibonacci",
        // The wait times are part of a progression series
        PROGRESSION: "Progression",
        // Wait times between retries are jittered to spread out the load
        JITTER: "Jitter",
        // The wait times follows a gamma distribution
        GAMMA: "Gamma",
    },
    // Function to calculate the retry interval based on the strategy used (in ms)
    getRetryInterval: (attempted, retryInterval, strategy, maxDelay = Infinity) => {
        if (retryInterval <= 0) throw new Error("retryInterval must be positive");
        let interval;
        switch (strategy) {
            case Retry.Strategy.EXPONENTIAL_BACKOFF:
                interval = retryInterval * Math.pow(2, attempted);
                break;
            case Retry.Strategy.RANDOM:
                interval = Math.floor(Math.random() * (retryInterval * Math.pow(2, attempted)));
                break;
            case Retry.Strategy.INCREMENTAL:
                interval = retryInterval * attempted;
                break;
            case Retry.Strategy.FIBONACCI:
                const fib = (function () {
                    let fibCache = [0, 1];
                    return function (n) {
                        if (fibCache[n] === undefined) {
                            fibCache[n] = fib(n - 1) + fib(n - 2);
                        }
                        return Math.min(fibCache[n], Number.MAX_SAFE_INTEGER / retryInterval); // Cap to prevent overflow
                    }
                })();
                interval = retryInterval * fib(attempted);
                break;
            case Retry.Strategy.PROGRESSION:
                interval = retryInterval + (attempted * attempted);
                break;
            case Retry.Strategy.JITTER:
                interval = retryInterval * Math.pow(2, attempted) * Math.random();
                break;
            case Retry.Strategy.GAMMA:
                if (attempted === 0) return retryInterval; // Avoid log(0)
                interval = retryInterval * (attempted * Math.log(attempted));
                break;
            default:
                interval = retryInterval;
        }
        return Math.min(interval, maxDelay); // Cap with optional maxDelay
    },
    // Synchronous sleep (busy wait) - WARNING: CPU-intensive; prefer attemptAsync for non-blocking delays
    sleep: (ms) => {
        const start = Date.now();
        while (Date.now() - start < ms) {}
    },
    // Function to attempt the provided action and retry if necessary using the prescribed strategy
    // Now handles if action is async (returns Promise)
    attempt: (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL,
              logger = console.log, maxDelay = Infinity) => {
        if (maxAttempts <= 0) throw new Error("maxAttempts must be positive");
        let exceptions = [];
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                const result = action();
                if (result instanceof Promise) {
                    // If action is async, await it synchronously (blocks thread)
                    let resolved;
                    result.then(res => resolved = res).catch(err => { throw err; });
                    while (resolved === undefined) Retry.sleep(1); // Busy wait for promise (not ideal)
                    return resolved;
                }
                return result; // Sync success
            } catch (ex) {
                exceptions.push(ex);
                if (attempted === maxAttempts - 1) {
                    logger('All attempts failed. Exceptions:', exceptions);
                    if (typeof AggregateError !== 'undefined') {
                        throw new AggregateError(exceptions, `All attempts failed after ${maxAttempts} retries`);
                    } else {
                        throw ex; // Fallback to last error
                    }
                }
                if (attempted > 0) {
                    const delay = Retry.getRetryInterval(attempted, retryInterval, strategy, maxDelay);
                    logger(`Failed, retrying in ${delay}ms: ${ex}`);
                    Retry.sleep(delay); // Add delay (busy wait)
                }
            }
        }
    },
    // Function to attempt the provided action in a non-blocking(async) manner and retry if necessary using the prescribed strategy
    attemptAsync: async (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL,
                         logger = console.log, maxDelay = Infinity) => {
        if (maxAttempts <= 0) throw new Error("maxAttempts must be positive");
        let exceptions = [];
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                if (attempted > 0) {
                    // If this is not the first attempt, wait for a certain duration based on the retry strategy
                    const delay = Retry.getRetryInterval(attempted, retryInterval, strategy, maxDelay);
                    await new Promise(resolve => setTimeout(resolve, delay));
                }
                return await action(); // If successful, return the result of the action.
            } catch (ex) {
                exceptions.push(ex);
                if (attempted === maxAttempts - 1) {
                    logger('All attempts failed. Exceptions:', exceptions);
                    // If all attempts fail, throw aggregated errors
                    if (typeof AggregateError !== 'undefined') {
                        throw new AggregateError(exceptions, `All attempts failed after ${maxAttempts} retries`);
                    } else {
                        throw new Error(`All attempts failed after ${maxAttempts} retries`);
                    }
                }
                else {
                    // Log the exception and the time to next retry
                    const delay = Retry.getRetryInterval(attempted, retryInterval, strategy, maxDelay);
                    logger(`Failed to complete action, retrying in ${delay}ms: ${ex}`);
                }
            }
        }
    }
};
