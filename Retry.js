// Object that contains retry related functions
const Retry = {
  // Object to store retry strategy names
  RetryStrategy: {
    FixedInterval: "FixedInterval",
    ExponentialBackOff: "ExponentialBackOff"
  },

  // Function to perform retries with a given action.
  // 'action' is the action to be performed,
  // 'retryInterval' is the interval between retries,
  // 'maxAttemptCount' is the maximum number of attempts (defaults to 3),
  // 'retryStrategy' is the strategy to use for retrying (defaults to FixedInterval)
  do: (action, retryInterval, maxAttemptCount = 3, retryStrategy = Retry.RetryStrategy.FixedInterval) => {
    // Array to store exceptions thrown during retries
    let exceptions = [];

    // Loop to perform retries
    for (let attempted = 0; attempted < maxAttemptCount; attempted++) {
      try {
        // Calculate the interval based on the retry strategy
        let interval = retryStrategy === Retry.RetryStrategy.ExponentialBackOff ?
          retryInterval * Math.pow(2, attempted) : retryInterval;
        
        // Wait for the interval before retrying (except for the first attempt)
        if (attempted > 0) {
          setTimeout(resolve, interval);
        }

        // Perform the action
        action();
        return;
      } catch (ex) {
        // Push the exception to the array of exceptions
        exceptions.push(ex);
        console.log(`Failed to complete action, retrying in ${retryInterval}: ${ex}`);
      }
    }

    // Throw an error containing the exceptions if all attempts have failed
    throw new Error(exceptions);
  },

  // Function to perform retries with an async action.
  // The parameters are the same as the 'do' function.
  async doAsync(action, retryInterval, maxAttemptCount = 3, retryStrategy = Retry.RetryStrategy.FixedInterval) {
    // Array to store exceptions thrown during retries
    let exceptions = [];

    // Loop to perform retries
    for (let attempted = 0; attempted < maxAttemptCount; attempted++) {
      try {
        // Calculate the interval based on the retry strategy
        let interval = retryStrategy === Retry.RetryStrategy.ExponentialBackOff ?
          retryInterval * Math.pow(2, attempted) : retryInterval;
        
        // Wait for the interval before retrying (except for the first attempt)
        if (attempted > 0) {
          await new Promise(resolve => setTimeout(resolve, interval));
        }

        // Perform the async action
        await action();
        return;
      } catch (ex) {
        // Push the exception to the array of exceptions
        exceptions.push(ex);
        console.log(`Failed to complete action, retrying in ${retryInterval}: ${ex}`);
      }
    }

    // Throw an error containing the exceptions if all attempts have failed
    throw new Error(exceptions);
  }
};
