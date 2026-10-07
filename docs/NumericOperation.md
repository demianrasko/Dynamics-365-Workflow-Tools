This step applies one operation to two numbers and returns a single result. [Numeric Functions](Numeric%20Functions.md) returns the sum, difference, product and quotient all at once; this step has only one output, so the wrong one can't be picked by mistake.

### Inputs

* **Number 1**: the first number.
* **Operation (+ - * / % min max)**: what to do with the two numbers:
  * `+` or `add`: Number 1 + Number 2
  * `-` or `subtract`: Number 1 - Number 2
  * `*`, `x` or `multiply`: Number 1 × Number 2
  * `/` or `divide`: Number 1 ÷ Number 2
  * `%` or `mod`: the remainder of Number 1 ÷ Number 2
  * `min` or `max`: the smaller or the larger of the two

  Names aren't case-sensitive, and spaces around them are ignored.
* **Number 2**: the second number.

### Outputs

* **Result**: the result of the operation.

### Notes

* The step fails with a clear message for an operation it doesn't know, or when Number 2 is 0 for `/` or `%` (Numeric Functions returns 0 for a division by 0 instead).
* Available in both the Dynamics 365 and the Power Platform versions.
