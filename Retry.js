const Retry = {
    // Define the retry strategies.
    Strategy: {
        FIXED_INTERVAL:      "FixedInterval",
        EXPONENTIAL_BACKOFF: "ExponentialBackOff"
    },
    // Method to get the retry interval based on the retry strategy.
    getRetryInterval: (attempted, retryInterval, strategy) => {
        // If retry strategy is Exponential Backoff, retry interval exponentially increases with each attempt.
        // Otherwise, retry interval is constant.
        return strategy === Retry.Strategy.EXPONENTIAL_BACKOFF
               ? retryInterval * Math.pow(2, attempted)
               : retryInterval;
    },
    // Method to retry a synchronous action.
    attempt: (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL) => {
        let exceptions = [];
        // Retry the action until max attempts is reached.
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                action();  // Perform the action.
                return;    // If it's successful, exit from the function.
            } catch (ex) {
                exceptions.push(ex); // In case of exception, store exception and retry.
                // If all attempts fail, log exceptions.
                if (attempted === maxAttempts - 1) {
                    console.log('All attempts failed. Exceptions:', exceptions);
                }
            }
        }
    },
    // Method to retry an asynchronous action.
    attemptAsync: async (action, retryInterval, maxAttempts = 3, strategy = Retry.Strategy.FIXED_INTERVAL) => {
        let exceptions = [];
        // Retry the action until max attempts is reached.
        for (let attempted = 0; attempted < maxAttempts; attempted++) {
            try {
                // If not first attempt, wait before retrying.
                if (attempted > 0) {
                    await new Promise(
                        resolve => setTimeout(resolve, Retry.getRetryInterval(attempted, retryInterval, strategy)));
                }
                await action(); // Perform the action.
                return;         // If it's successful, exit from the function.
            } catch (ex) {
                exceptions.push(ex); // In case of exception, store exception and retry.
                // If all attempts fail, log exceptions and throw an error.
                if (attempted === maxAttempts - 1) {
                    console.log('All attempts failed. Exceptions:', exceptions);
                    throw new Error(`All attempts failed after ${maxAttempts} retries`);
                }
                else {
                    // In development environment, log the exceptions for debugging.
                    if (developmentEnvironment) {
                        console.log(
                            `Failed to complete action, retrying in ${Retry.getRetryInterval(attempted, retryInterval,
                                                                                             strategy)}: ${ex}`);
                    }
                }
            }
        }
    }
};
