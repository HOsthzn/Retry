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
    // Function to calculate the retry interval based on the strategy used
    getRetryInterval: (attempted, retryInterval, strategy) => {
        switch (strategy) {
            case Retry.Strategy.EXPONENTIAL_BACKOFF:
                return retryInterval * Math.pow(2, attempted);
            case Retry.Strategy.RANDOM:
                return Math.floor(Math.random() * (retryInterval * Math.pow(2, attempted)));
            case Retry.Strategy.INCREMENTAL:
                return retryInterval * attempted;
            case Retry.Strategy.FIBONACCI:
                const fib = (function () {
                    let fibCache = [0, 1];
                    return function (n) {
                        if (fibCache[n] === undefined) {
                            fibCache[n] = fib(n - 1) + fib(n - 2);
                        }
                        return fibCache[n];
                    }
                })();
                return retryInterval * fib(attempted);
            case Retry.Strategy.PROGRESSION:
                return retryInterval + (attempted * attempted);
            case Retry.Strategy.JITTER:
                return retryInterval * Math.pow(2, attempted) * Math.random();
            case Retry.Strategy.GAMMA:
                return retryInterval * (attempted * Math.log(attempted));
            default:
                return retryInterval;
        }
    },
    // Function to attempt the provided action and retry if necessary using the prescribed strategy
    attempt: (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL,
              logger                                           = console.log) => {
        // Array to store any exceptions encountered during attempt
        let exceptions = [];
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                return action(); // If successful, return the result of the action.
            } catch (ex) {
                exceptions.push(ex);
                if (attempted === maxAttempts - 1) {
                    logger('All attempts failed. Exceptions:', exceptions);
                    throw ex; // If all attempts fail, throw the latest exception.
                }
            }
        }
    },
    // Function to attempt the provided action in a non-blocking(async) manner and retry if necessary using the prescribed strategy
    attemptAsync: async (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL,
                         logger                                           = console.log) => {
        // Array to store any exceptions encountered during attempt
        let exceptions = [];
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                if (attempted > 0) {
                    // If this is not the first attempt, wait for a certain duration based on the retry strategy
                    await new Promise(
                        resolve => setTimeout(resolve, Retry.getRetryInterval(attempted, retryInterval, strategy)));
                }
                return await action(); // If successful, return the result of the action.
            } catch (ex) {
                exceptions.push(ex);
                if (attempted === maxAttempts - 1) {
                    logger('All attempts failed. Exceptions:', exceptions);
                    // If all attempts fail, throw a new error stating the failure.
                    throw new Error(`All attempts failed after ${maxAttempts} retries`);
                }
                else {
                    // Log the exception and the time to next retry
                    logger(`Failed to complete action, retrying in ${Retry.getRetryInterval(attempted, retryInterval,
                                                                                            strategy)}: ${ex}`);
                }
            }
        }
    }
};
