using NUnit.Framework.Constraints;
using System.ComponentModel;
using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        /// <summary>
        /// Stores differential equations in the form of function equations.
        /// </summary>
        public class Expression {
            /// <summary>
            /// Stores the function type at this level of the parse tree.
            /// </summary>
            private String fn;

            /// <summary>
            /// Stores all <c>Expression</c> children.
            /// </summary>
            private readonly List<Expression> children;

            /// <summary>
            /// Creates a new <c>Expression</c> object.
            /// </summary>
            public Expression() {
                this.children = new();
                this.fn = "0";
            }

            /// <summary>
            /// Creates a new <c>Expression</c> object.
            /// </summary>
            /// <param name="fn">
            /// The given <c>Expression</c> as a <c>string</c>.
            /// </param>
            public Expression(String fn) {
                this.children = new();
                this.fn = "";
                Formulate(fn, FunctionOperator.ADDITION);
                Simplify(true);
            }

            /// <summary>
            /// Parses a <c>String</c> into this <c>Expression</c>.
            /// </summary>
            /// <param name="fn">
            /// The <c>String</c> to be parsed.
            /// </param>
            /// <param name="fop">
            /// The target operation.
            /// </param>
            private void Formulate(String fn, FunctionOperator fop) {
                switch (fop) {
                    case FunctionOperator.INTEGER_CONSTANT:
                    case FunctionOperator.VARIABLE:
                        FormulateConstant(fn, fop);
                        break;
                    case FunctionOperator.DIFFERENTIAL:
                    case FunctionOperator.PARENTHETICAL:
                    case FunctionOperator.NEGATION:
                        FormulateUnaryOperator(fn, fop);
                        break;
                    case FunctionOperator.MULTIPLICATION:
                    case FunctionOperator.ADDITION:
                    case FunctionOperator.SUBTRACTION:
                    case FunctionOperator.DIVISION:
                        FormulateBinaryOperator(fn, fop);
                        break;
                    default:
                        throw new("Invalid operator.");
                }
            }

            /// <summary>
            /// Parses a variable or integer constant value.
            /// </summary>
            /// <param name="fn">
            /// The <c>String</c> to be parsed.
            /// </param>
            /// <param name="fop">
            /// The target operation.
            /// </param>
            private void FormulateConstant(String fn, FunctionOperator fop) {
                VerifyLength(fn);
                foreach (char c in fn.ToCharArray()) {
                    if (fop == FunctionOperator.INTEGER_CONSTANT & !Char.IsDigit(c)) {
                        fop = FunctionOperator.VARIABLE;
                    }
                    if (fop == FunctionOperator.VARIABLE & !Char.IsLetterOrDigit(c)) {
                        throw new();
                    }
                }
                this.fn = fn;
            }

            /// <summary>
            /// Parses an expression holding a unary operation.
            /// </summary>
            /// <param name="fn">
            /// The <c>String</c> to be parsed.
            /// </param>
            /// <param name="fop">
            /// The target operation.
            /// </param>
            private void FormulateUnaryOperator(String fn, FunctionOperator fop) {
                VerifyLength(fn);
                Expression child = new();
                switch (fop) {
                    case FunctionOperator.DIFFERENTIAL:
                        if (fn.EndsWith("'")) {
                            child.FormulateConstant(fn[..^1], FunctionOperator.VARIABLE);
                        } else {
                            child.FormulateUnaryOperator(fn, FunctionOperator.PARENTHETICAL);
                        }
                        break;
                    case FunctionOperator.PARENTHETICAL:
                        if (fn.StartsWith("(") & fn.EndsWith(")")) {
                            child.FormulateBinaryOperator(fn[1..^1], FunctionOperator.ADDITION);
                        } else {
                            child.FormulateConstant(fn, FunctionOperator.INTEGER_CONSTANT);
                        }
                        break;
                    case FunctionOperator.NEGATION:
                        if (fn.StartsWith("-")) {
                            child.FormulateConstant(fn[1..], FunctionOperator.NEGATION);
                        } else {
                            child.FormulateUnaryOperator(fn, FunctionOperator.DIFFERENTIAL);
                        }
                        break;
                    default:
                        throw new ArgumentException("Must be unary operator.");
                }
                this.fn = child.fn;
                this.children.Clear();
                this.children.AddRange(child.children);
            }

            /// <summary>
            /// Parses an expression holding a binary operation.
            /// </summary>
            /// <param name="fn">
            /// The <c>String</c> to be parsed.
            /// </param>
            /// <param name="fop">
            /// The target operation.
            /// </param>
            private void FormulateBinaryOperator(String fn, FunctionOperator fop) {
                VerifyLength(fn);
                FunctionOperator next = FunctionOperator.SUBTRACTION;
                switch (fop) {
                    case FunctionOperator.SUBTRACTION:
                        next = FunctionOperator.MULTIPLICATION;
                        break;
                    case FunctionOperator.MULTIPLICATION:
                        next = FunctionOperator.DIVISION;
                        break;
                    case FunctionOperator.DIVISION:
                        next = FunctionOperator.NEGATION;
                        break;
                }
                char op = GetCharOperator(fop);
                char[] chars = fn.ToCharArray();
                bool proceed = true;
                for (int i = 0; i < chars.Length; i++) {
                    if (chars[i] == op) {
                        try {
                            SplitBinaryExpression(fn, i, fop, next);
                            proceed = false;
                        } catch (Exception) { }
                    }
                }
                if (proceed) {
                    Formulate(fn, next);
                }
            }

            /// <summary>
            /// Splits a binary expression to continue parsing on lower levels.
            /// </summary>
            /// <param name="fn">
            /// The <c>Expression</c> string.
            /// </param>
            /// <param name="index">
            /// The nextIndex of the delimiter character.
            /// </param>
            /// <param name="o1">
            /// The <c>FunctionOperator</c> to be used for the first child.
            /// </param>
            /// <param name="o2">
            /// The <c>FunctionOperator</c> to be used for the second child.
            /// </param>
            private void SplitBinaryExpression(String fn, int index, FunctionOperator o1, FunctionOperator o2) {
                Expression child1 = new(), child2 = new();
                child1.Formulate(fn[0..^index], o1);
                child2.Formulate(fn[(index + 1)..], o2);
                AddChild(child1);
                AddChild(child2);
                AssignOperator(o1);
            }

            /// <summary>
            /// Adds a child <c>Expression</c> to this object.
            /// </summary>
            /// <param name="fn">
            /// The new child.
            /// </param>
            private void AddChild(Expression fn) {
                this.children.Add(fn);
            }

            /// <summary>
            /// Assigns this <c>Expression</c> an operator depending on the
            /// <c>FunctionOperator</c> used.
            /// </summary>
            /// <param name="fop">
            /// The target <c>FunctionOperator</c>.
            /// </param>
            private void AssignOperator(FunctionOperator fop) {
                this.fn = GetCharOperator(fop) + "";
            }

            /// <summary>
            /// Asserts a positive length for the Expression <c>String</c>.
            /// </summary>
            /// <param name="fn">
            /// The <c>Expression</c> as a <c>String</c>.
            /// </param>
            private static void VerifyLength(String fn) {
                if (fn.Length == 0) {
                    throw new();
                }
            }

            /// <summary>
            /// Gets the <c>char</c> binary operator associated with a particular
            /// <c>FunctionOperator</c>.
            /// </summary>
            /// <param name="fop">
            /// The provided <c>FunctionOperator</c>.
            /// </param>
            /// <returns>
            /// The converted <c>char</c> value.
            /// </returns>
            private static char GetCharOperator(FunctionOperator fop) {
                return (char) (int) fop;
            }

            /// <summary>
            /// Gets the <c>FunctionOperator</c> associated with a particular
            /// <c>char</c> value.
            /// </summary>
            /// <param name="fop">
            /// The provided <c>char</c> value.
            /// </param>
            /// <returns>
            /// The associated <c>FunctionOperator</c>.
            /// </returns>
            private static FunctionOperator GetOperatorObject(char fop) {
                return (FunctionOperator) (int) fop;
            }

            /// <summary>
            /// Simplifies this <c>Expression</c>.
            /// </summary>
            /// <param name="simplifyAllLayers">
            /// <c>true</c> if the simplification process should continue 
            /// throughout the whole <c>Expression</c>, else <c>false</c>
            /// if the process should be confined to this index.
            /// </param>
            public void Simplify(bool simplifyAllLayers) {
                Stack<Expression> fnStack = new(new[] { this });
                Stack<bool> checkStack = new(new[] { false });
                Stack<bool> simplifyChildrenStack = new(new[] { simplifyAllLayers });
                while (fnStack.Count > 0) {
                    Expression targetFn = fnStack.Pop();
                    bool simplifyChildren = simplifyChildrenStack.Pop();
                    if (checkStack.Pop()) {
                        String fn = targetFn.fn;
                        switch (GetOperatorObject(fn[0])) {
                            case FunctionOperator.ADDITION:
                                List<Expression> aChildren = new(), sChildren = new();
                                foreach (Expression child in targetFn.children) {
                                    String childFn = child.fn;
                                    switch (GetOperatorObject(childFn[0])) {
                                        case FunctionOperator.ADDITION:
                                        case FunctionOperator.PARENTHETICAL:
                                            aChildren.AddRange(child.children);
                                            break;
                                        case FunctionOperator.ZERO:
                                            break; // does nothing
                                        case FunctionOperator.NEGATION:
                                            sChildren.Add(child.children[0]);
                                            break;
                                        default:
                                            aChildren.Add(child);
                                            break;
                                    }
                                }
                                if (aChildren.Count == 0) {
                                    aChildren.Add(new Expression());
                                }
                                if (aChildren.Count == 1) {
                                    targetFn.AssignValues(aChildren[0].fn, aChildren[0].children);
                                } else {
                                    targetFn.AssignValues(targetFn.fn, aChildren);
                                }
                                if (sChildren.Count > 0) {
                                    foreach (Expression subChild in sChildren) {
                                        Expression child = new();
                                        child.AssignValues(targetFn.fn, targetFn.children);
                                        targetFn.AssignValues("-", new(new[] { child, subChild }));
                                    }
                                    fnStack.Push(targetFn);
                                    checkStack.Push(false);
                                    simplifyChildrenStack.Push(true);
                                }
                                break;
                            case FunctionOperator.SUBTRACTION:
                                if (targetFn.children[0].Equals(targetFn.children[1])) {
                                    targetFn.AssignValues("0", new());
                                } else if (targetFn.children[0].fn.Equals("0")) {
                                    targetFn.AssignValues("!", new(new[] { targetFn.children[1] }));
                                } else if (targetFn.children[1].fn.Equals("!")) {
                                    targetFn.children[1] = targetFn.children[1].children[0];
                                    targetFn.fn = "+";
                                }
                                break;
                            case FunctionOperator.MULTIPLICATION:
                                List<Expression> mChildren = new();
                                bool isZero = false;
                                foreach (Expression child in targetFn.children) {
                                    String childFn = child.fn;
                                    switch (GetOperatorObject(childFn[0])) {
                                        case FunctionOperator.MULTIPLICATION:
                                            mChildren.AddRange(child.children);
                                            break;
                                        case FunctionOperator.PARENTHETICAL:
                                            Expression grandchild = child.children[0];
                                            String grandchildFn = grandchild.fn;
                                            switch (GetOperatorObject(grandchildFn[0])) {
                                                case FunctionOperator.ADDITION:
                                                case FunctionOperator.SUBTRACTION:
                                                    break;
                                                default:
                                                    mChildren.AddRange(grandchild.children);
                                                    break;
                                            }
                                            break;
                                        case FunctionOperator.ONE: 
                                            break; // does nothing
                                        case FunctionOperator.ZERO: 
                                            isZero = true;
                                            break; // whole expression becomes zero
                                        default:
                                            mChildren.Add(child);
                                            break;
                                    }
                                }
                                if (isZero) {
                                    mChildren.Clear();
                                    mChildren.Add(new Expression("0"));
                                }
                                targetFn.AssignValues(targetFn.fn, mChildren);
                                break;
                            case FunctionOperator.PARENTHETICAL:
                                Expression pChild = targetFn.children[0];
                                if (pChild.fn[0] == '(' | Char.IsLetterOrDigit(pChild.fn[0])) {
                                    targetFn.AssignValues(pChild.fn, pChild.children);
                                }
                                break;
                            case FunctionOperator.DIVISION:
                                if (targetFn.children[0].Equals(targetFn.children[1])) {
                                    targetFn.AssignValues("1", new());
                                } else if (targetFn.children[0].fn.Equals("0")) {
                                    targetFn.AssignValues("0", new());
                                }
                                break;
                            case FunctionOperator.NEGATION:
                                Expression nChild = targetFn.children[0];
                                switch (GetOperatorObject(nChild.fn[0])) {
                                    case FunctionOperator.NEGATION:
                                        Expression grandchild = nChild.children[0];
                                        targetFn.AssignValues(grandchild.fn, grandchild.children);
                                        break;
                                    case FunctionOperator.PARENTHETICAL:
                                        Expression pGrandchild = nChild.children[0];
                                        if (pGrandchild.fn[0] == '-') {
                                            Expression greatGrandchild = pGrandchild.children[0];
                                            targetFn.AssignValues(greatGrandchild.fn, greatGrandchild.children);
                                        }
                                        break;
                                    case FunctionOperator.ZERO:
                                        targetFn.AssignValues("0", new());
                                        break;
                                }
                                break;
                        }
                    } else {
                        fnStack.Push(targetFn);
                        checkStack.Push(true);
                        simplifyChildrenStack.Push(simplifyChildren);
                        if (simplifyAllLayers) {
                            foreach (Expression child in targetFn.children) {
                                fnStack.Push(child);
                                checkStack.Push(false);
                                simplifyChildrenStack.Push(simplifyChildren);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Assigns specific fields to this <c>Expression</c>.
            /// </summary>
            /// <param name="fn">
            /// The new <c>Expression</c> classification.
            /// </param>
            /// <param name="children">
            /// The new list of child <c>Expression</c> objects.
            /// </param>
            private void AssignValues(String fn, List<Expression> children) {
                this.fn = fn;
                this.children.Clear();
                this.children.AddRange(children);
            }

            /// <summary>
            /// Determines whether this <c>Expression</c> is a constant.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this <c>Expression</c> is a integer constant, else 
            /// <c>false</c>.
            /// </returns>
            public bool IsIntegerConstant() {
                foreach (char c in this.fn.ToCharArray()) {
                    if (!Char.IsDigit(c)) {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Determines whether this <c>Expression</c> is a variable.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this <c>Expression</c> is a variable, else 
            /// <c>false</c>.
            /// </returns>
            public bool IsVariable() {
                if (!Char.IsDigit(this.fn[0])) {
                    return false;
                }
                foreach (char c in this.fn.ToCharArray()) {
                    if (Char.IsLetter(c)) {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Counts the number of instances of a specified variable
            /// <c>Expression</c> contained in this <c>Expression</c>.
            /// </summary>
            /// <param name="var">
            /// The target variable.
            /// </param>
            /// <param name="includeDifferentials">
            /// <c>true</c> if the count includes derivatives of the target
            /// variable, else <c>false</c>.
            /// </param>
            /// <returns>
            /// The number of occurrences of the target variable.
            /// </returns>
            public int CountInstances(Expression var, bool includeDifferentials) {
                if (!var.IsVariable()) {
                    throw new ArgumentException("Input must be a variable.");
                } else if ((!includeDifferentials) & IsDifferential()) {
                    return 0;
                }
                String varFn = var.fn;
                int count = 0;
                Stack<Expression> thisStack = new();
                thisStack.Push(this);
                while (thisStack.Count > 0) {
                    Expression fn = thisStack.Pop();
                    if (fn.fn.Equals(varFn)) {
                        count++;
                    } else {
                        foreach (Expression child in fn.children) {
                            if (includeDifferentials | !child.IsDifferential()) {
                                thisStack.Push(child);
                            }
                        }
                    }
                }
                return count;
            }

            /// <summary>
            /// Equates this <c>Expression</c> with another specified <c>Expression</c>
            /// and isolates a specified variable. This variable must occur exactly
            /// once in this <c>Expression</c> and nowhere in the input <c>Expression</c>.
            /// </summary>
            /// <param name="o">
            /// The equated <c>Expression</c>.
            /// </param>
            /// <param name="var">
            /// The target variable.
            /// </param>
            /// <returns>
            /// The <c>Expression</c> equation as solved for the variable.
            /// </returns>
            public Expression Isolate(Expression o, Expression var) {
                Expression thisCopy = Copy();
                Expression oCopy = o.Copy();
                foreach (int index in GetListOfPathIndices(var)) {
                    String fn = thisCopy.fn;
                    Expression targetChild = thisCopy.children[index];
                    switch ((FunctionOperator) fn[0]) {
                        case FunctionOperator.ADDITION:
                            thisCopy.children.RemoveAt(index);
                            oCopy = oCopy.Subtract(thisCopy);
                            break;
                        case FunctionOperator.SUBTRACTION:
                            if (index == 0) {
                                oCopy = oCopy.Add(thisCopy.children[1]);
                            } else {
                                oCopy = thisCopy.children[0].Subtract(oCopy);
                            }
                            break;
                        case FunctionOperator.MULTIPLICATION:
                            thisCopy.children.RemoveAt(index);
                            oCopy = oCopy.Divide(thisCopy);
                            break;
                        case FunctionOperator.DIVISION:
                            if (index == 0) {
                                oCopy = oCopy.Multiply(thisCopy.children[1]);
                            } else {
                                oCopy = thisCopy.children[0].Divide(oCopy);
                            }
                            break;
                        case FunctionOperator.NEGATION:
                            oCopy = oCopy.Negate();
                            break;
                        case FunctionOperator.PARENTHETICAL:
                            InsertParentheticalExpression(oCopy);
                            break;
                    }
                    thisCopy = targetChild;
                }
                return oCopy;
            }

            /// <summary>
            /// Gets the list of path indices leading from the base index
            /// of this <c>Expression</c> to a specified variable. This method
            /// assumes that the variable occurs at most once in the 
            /// <c>Expression</c> and does not enter differential <c>Expression</c> 
            /// objects.
            /// </summary>
            /// <param name="var">
            /// The specified variable.
            /// </param>
            /// <returns>
            /// The list of indices.
            /// </returns>
            private List<int> GetListOfPathIndices(Expression var) {
                var.AssertVariable();
                AssertNotDifferential();
                Stack<Expression> fnStack = new();
                Stack<bool> checkStack = new();
                Stack<int> indexStack = new();
                fnStack.Push(this);
                checkStack.Push(true);
                indexStack.Push(0);
                List<int> indices = new() { 0 };
                String varFn = var.fn;
                while (fnStack.Count > 0) {
                    Expression fn = fnStack.Pop();
                    int index = indexStack.Pop();
                    if (checkStack.Pop()) {
                        if (fn.fn.Equals(varFn)) {
                            indices.Reverse();
                            return indices;
                        } else {
                            fnStack.Push(new());
                            indexStack.Push(0);
                            checkStack.Push(false);
                            int nextIndex = 0;
                            foreach (Expression child in fn.children) {
                                if (!child.IsDifferential()) {
                                    fnStack.Push(child);
                                    indexStack.Push(nextIndex);
                                    checkStack.Push(true);
                                }
                                nextIndex++;
                            }
                            indices.Insert(0, index);
                        }
                    } else {
                        indices.RemoveAt(0);
                    }
                }
                throw new ArgumentException("Variable does not occur in path");
            }

            /// <summary>
            /// Makes a copy of this <c>Expression</c>.
            /// </summary>
            /// <returns>
            /// The copy.
            /// </returns>
            public Expression Copy() {
                Expression copy = new();
                Stack<Expression> thisStack = new();
                Stack<Expression> copyStack = new();
                thisStack.Push(this);
                copyStack.Push(copy);
                while (thisStack.Count > 0) {
                    Expression targetThis = thisStack.Pop();
                    Expression targetCopy = copyStack.Pop();
                    targetCopy.fn = targetThis.fn;
                    foreach (Expression child in targetThis.children) {
                        thisStack.Push(child);
                        Expression nextCopy = new();
                        copyStack.Push(nextCopy);
                        targetCopy.AddChild(nextCopy);
                    }
                }
                return copy;
            }

            /// <summary>
            /// Substitutes a specified variable in this <c>Expression</c>
            /// with a different <c>Expression</c>.
            /// </summary>
            /// <param name="var">
            /// The target variable.
            /// </param>
            /// <param name="fn">
            /// The replacement <c>Expression</c>.
            /// </param>
            public void Substitute(Expression var, Expression fn) {
                var.AssertVariable();
                if (!IsDifferential()) {
                    String varFn = var.fn;
                    Stack<Expression> thisStack = new();
                    thisStack.Push(this);
                    while (thisStack.Count > 0) {
                        Expression target = thisStack.Pop();
                        if (target.fn.Equals(varFn)) {
                            target.AssignValues("(", new() { fn });
                            target.Simplify(false);
                        } else {
                            foreach (Expression child in target.children) {
                                if (!child.IsDifferential()) {
                                    thisStack.Push(child);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Determines whether <c>this</c> is a variable <c>Expression</c.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// If <c>this</c> is not a variable.
            /// </exception>
            private void AssertVariable() {
                if (!IsVariable()) {
                    throw new ArgumentException("Expression must be variable.");
                }
            }

            /// <summary>
            /// Determines whether this <c>Expression</c> is a differential.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this is a differential, else <c>false</c>.
            /// </returns>
            private bool IsDifferential() {
                return this.fn[0] == '\'';
            }

            /// <summary>
            /// Asserts that <c>this</c> is not a differential <c>Expression</c>.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// If <c>this</c> is a differential <c>Expression</c>.
            /// </exception>
            private void AssertNotDifferential() {
                if (IsDifferential()) {
                    throw new ArgumentException("Expression cannot be a differential.");
                }
            }

            /// <summary>
            /// Simplifies this Expression and its first layer of children.
            /// </summary>
            private void SimplifyChildren() {
                foreach (Expression child in this.children) {
                    child.Simplify(false);
                }
                Simplify(false);
            }

            /// <summary>
            /// Adds two <c>Expression</c> objects.
            /// </summary>
            /// <param name="addend">
            /// The subtrahend <c>Expression</c>.
            /// </param>
            /// <returns>
            /// <c>this + subtrahend</c>
            /// </returns>
            public Expression Add(Expression addend) {
                Expression sum = new();
                sum.AssignValues("+", new() { Copy(), addend.Copy() });
                sum.SimplifyChildren();
                return sum;
            }

            /// <summary>
            /// Adds two <c>Expression</c> objects.
            /// </summary>
            /// <param name="subtrahend">
            /// The subtrahend <c>Expression</c>.
            /// </param>
            /// <returns>
            /// <c>this + subtrahend</c>
            /// </returns>
            public Expression Subtract(Expression subtrahend) {
                Expression newSub = subtrahend.Copy();
                int compare = CompareOperators(FunctionOperator.SUBTRACTION, GetOperatorObject(newSub.fn[0]));
                if (compare >= 0) {
                    InsertParentheticalExpression(newSub);
                }
                Expression difference = new();
                difference.AssignValues("-", new() {Copy(), newSub});
                difference.SimplifyChildren();
                return difference;
            }

            /// <summary>
            /// Multiplies two <c>Expression</c> objects.
            /// </summary>
            /// <param name="multiplicand">
            /// The multiplicand <c>Expression</c>.
            /// </param>
            /// <returns>
            /// <c>this * multiplicand</c>
            /// </returns>
            public Expression Multiply(Expression multiplicand) {
                Expression thisCopy = Copy();
                Expression multCopy = multiplicand.Copy();
                int compareThis = CompareOperators(FunctionOperator.MULTIPLICATION, GetOperatorObject(thisCopy.fn[0]));
                int compareMult = CompareOperators(FunctionOperator.MULTIPLICATION, GetOperatorObject(multCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                if (compareMult > 0) {
                    InsertParentheticalExpression(multCopy);
                }
                Expression product = new();
                product.AssignValues("*", new() { thisCopy, multCopy});
                product.SimplifyChildren();
                return product;
            }

            /// <summary>
            /// Divides this <c>Expression</c> by another specified <c>Expression</c>.
            /// </summary>
            /// <param name="divisor">
            /// The divisor <c>Expression</c>.
            /// </param>
            /// <returns>
            /// <c>this / divisor</c>
            /// </returns>
            public Expression Divide(Expression divisor) {
                Expression thisCopy = Copy();
                Expression diviCopy = divisor.Copy();
                int compareThis = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(thisCopy.fn[0]));
                int compareDivi = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(diviCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                if (compareDivi > 0) {
                    InsertParentheticalExpression(diviCopy);
                }
                Expression quotient = new();
                quotient.AssignValues("/", new() { thisCopy, diviCopy });
                quotient.SimplifyChildren();
                return quotient;
            }

            /// <summary>
            /// Negates this <c>Expression</c>.
            /// </summary>
            /// <returns>
            /// <c>-this</c>
            /// </returns>
            public Expression Negate() {
                Expression thisCopy = Copy();
                int compareThis = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(thisCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                Expression negation = new();
                negation.AssignValues("!", new() { thisCopy });
                negation.SimplifyChildren();
                return negation;
            }

            /// <summary>
            /// Packages this <c>Expression</c> inside a parenthetical
            /// <c>Expression</c>.
            /// </summary>
            /// <param name="fn">
            /// The <c>Expression</c> to be packaged.
            /// </param>
            private static void InsertParentheticalExpression(Expression fn) {
                String fnStr = fn.fn;
                List<Expression> children = new();
                children.AddRange(fn.children);
                Expression replacement = new();
                replacement.AssignValues(fnStr, children);
                fn.AssignValues("(", new() { replacement });
            }

            /// <summary>
            /// Compares two <c>FunctionOperators</c> for precedence in 
            /// the standard order of operations.
            /// </summary>
            /// <param name="f1">
            /// The first operator.
            /// </param>
            /// <param name="f2">
            /// The second operator.
            /// </param>
            /// <returns>
            /// <c>1</c> if <c>f1</c> takes precedence over <c>f2</c>,
            /// else <c>0</c> if <c>f1 = f2</c>, else <c>-1</c> if
            /// <c>f2</c> takes precedence over <c>f1</c>.
            /// </returns>
            private static int CompareOperators(FunctionOperator f1, FunctionOperator f2) {
                return (GetComparatorOperator(f1) - GetComparatorOperator(f2)) switch {
                    <0 => -1, 0 => 0, >0 => 1
                };
            }

            /// <summary>
            /// Gets the comparator operator int for a specified 
            /// <c>FunctionOperator</c>. This int 
            /// </summary>
            /// <param name="fop">
            /// The specified <c>FunctionOperator</c>.
            /// </param>
            /// <returns>
            /// The int value.
            /// </returns>
            private static int GetComparatorOperator(FunctionOperator fop) {
                return fop switch {
                    FunctionOperator.ADDITION => 1,
                    FunctionOperator.SUBTRACTION => 2,
                    FunctionOperator.MULTIPLICATION => 3,
                    FunctionOperator.DIVISION => 4,
                    FunctionOperator.NEGATION => 5,
                    FunctionOperator.DIFFERENTIAL => 6,
                    FunctionOperator.PARENTHETICAL => 7,
                    _ => 7
                };
            }

            /// <summary>
            /// Gets the hashCode for this <c>Expression</c>.
            /// </summary>
            /// <returns>
            /// An identifier to be used in hash tables.
            /// </returns>
            public override int GetHashCode() {
                AssertVariable();
                return HashCode.Combine(this.ToString());
            }

            /// <summary>
            /// Converts this <c>Expression</c> to a printable format that exposes
            /// its parse tree.
            /// </summary>
            /// <returns>The string parse tree.</returns>
            public string ToTree() {
                //return ToTree("");
                StringBuilder builder = new();
                Stack<Expression> fnStack = new(new[] { this });
                Stack<string> indentStack = new(new[] { "" });
                while (fnStack.Count > 0) {
                    Expression fn = fnStack.Pop();
                    string indent = indentStack.Pop();
                    builder.Append(indent).Append(fn.fn);
                    Stack<Expression> proxyStack = new();
                    foreach (Expression child in fn.children) {
                        proxyStack.Push(child);
                    }
                    string nextIndent = indent + '\t';
                    while (proxyStack.Count > 0) {
                        fnStack.Push(proxyStack.Pop());
                        indentStack.Push(nextIndent);
                    }
                }
                return builder.ToString();
            }

            /// <summary>
            /// Converts this <c>Expression</c> to a printable format that exposes
            /// its parse tree.
            /// </summary>
            /// <param name="indent">The indent placed before every item in this
            /// list, exposing its displacement in the tree from the parent.</param>
            /// <returns></returns>
            private string ToTree(string indent) {
                string print = indent + this.fn;
                indent += "\t";
                foreach (Expression child in this.children) {
                    print = print + "\n" + child.ToTree(indent);
                }
                return print;
            }

            /// <summary>
            /// Converts this <c>Expression</c> to a printable format.
            /// </summary>
            /// <returns>
            /// This <c>Expression</c> as a <c>String</c>.
            /// </returns>
            public override string ToString() {
                String fn = this.fn;
                char op = fn[0];
                switch ((FunctionOperator) op) {
                    case FunctionOperator.ADDITION:
                    case FunctionOperator.MULTIPLICATION:
                        StringBuilder builder = new();
                        string opString = op + "", delimiter = "";
                        foreach (Expression child in this.children) {
                            builder.Append(delimiter).Append(child);
                            delimiter = opString;
                        }
                        fn = builder.ToString();
                        break;
                    case FunctionOperator.SUBTRACTION:
                    case FunctionOperator.DIVISION:
                        fn = this.children[0].ToString() + op + this.children[1];
                        break;
                    case FunctionOperator.PARENTHETICAL:
                        fn = "(" + this.children[0] + ')';
                        break;
                    case FunctionOperator.DIFFERENTIAL:
                        fn = this.children[0].fn + "'";
                        break;
                    case FunctionOperator.NEGATION:
                        fn = "-" + this.children[0];
                        break;
                }
                return fn;  // If this statement is reached, this Expression
            }               // is either a constant or a variable

            /// <summary>
            /// Stores each <c>FunctionOperator</c> with its associated ASCII code.
            /// </summary>
            private enum FunctionOperator {
                NEGATION                    = 33, //    !
                INTEGER_CONSTANT            = 35, //    #
                VARIABLE                    = 38, //    &
                DIFFERENTIAL                = 39, //    '
                PARENTHETICAL               = 40, //    (
                MULTIPLICATION              = 42, //    *
                ADDITION                    = 43, //    +
                SUBTRACTION                 = 45, //    -
                DIVISION                    = 47, //    /
                ZERO                        = 48, //    0
                ONE                         = 49  //    1
            }
        }
    }
}
