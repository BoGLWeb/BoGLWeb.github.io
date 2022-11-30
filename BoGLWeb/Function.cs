using NUnit.Framework.Constraints;
using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        /// <summary>
        /// Stores differential equations in the form of function equations.
        /// </summary>
        public class Function {
            /// <summary>
            /// Stores the function type at this level of the parse tree.
            /// </summary>
            private String fn;

            /// <summary>
            /// Stores all <c>Function</c> children.
            /// </summary>
            private readonly List<Function> children;

            /// <summary>
            /// Creates a new <c>Function</c> object.
            /// </summary>
            public Function() {
                this.children = new();
                this.fn = "";
            }

            /// <summary>
            /// Creates a new <c>Function</c> object.
            /// </summary>
            /// <param name="fn">
            /// The given <c>Function</c> as a <c>string</c>.
            /// </param>
            public Function(String fn) {
                this.children = new();
                this.fn = "";
                Formulate(fn, FunctionOperator.ADDITION);
                Simplify(true);
            }

            /// <summary>
            /// Parses a <c>String</c> into this <c>Function</c>.
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
                Function child = new();
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
                AddChild(child);
                AssignOperator(fop);
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
                FunctionOperator next = fop;
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
            /// The <c>Function</c> string.
            /// </param>
            /// <param name="index">
            /// The index of the delimiter character.
            /// </param>
            /// <param name="o1">
            /// The <c>FunctionOperator</c> to be used for the first child.
            /// </param>
            /// <param name="o2">
            /// The <c>FunctionOperator</c> to be used for the second child.
            /// </param>
            private void SplitBinaryExpression(String fn, int index, FunctionOperator o1, FunctionOperator o2) {
                Function child1 = new(), child2 = new();
                child1.Formulate(fn[0..^index], o1);
                child2.Formulate(fn[(index + 1)..], o2);
                AddChild(child1);
                AddChild(child2);
                AssignOperator(o1);
            }

            /// <summary>
            /// Adds a child <c>Function</c> to this object.
            /// </summary>
            /// <param name="fn">
            /// The new child.
            /// </param>
            private void AddChild(Function fn) {
                this.children.Add(fn);
            }

            /// <summary>
            /// Assigns this <c>Function</c> an operator depending on the
            /// <c>FunctionOperator</c> used.
            /// </summary>
            /// <param name="fop">
            /// The target <c>FunctionOperator</c>.
            /// </param>
            private void AssignOperator(FunctionOperator fop) {
                this.fn = GetCharOperator(fop) + "";
            }

            /// <summary>
            /// Asserts a positive length for the Function <c>String</c>.
            /// </summary>
            /// <param name="fn">
            /// The <c>Function</c> as a <c>String</c>.
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
            /// Simplifies this <c>Function</c>.
            /// </summary>
            /// <param name="proceedThroughLayers">
            /// <c>true</c> if the simplification process should continue 
            /// throughout the whole <c>Function</c>, else <c>false</c>
            /// if the process should be confined to this layer.
            /// </param>
            public void Simplify(bool simplifyAllLayers) {
                Stack<Function> fnStack = new();
                Stack<bool> checkStack = new();
                fnStack.Push(this);
                checkStack.Push(false);
                while (fnStack.Count > 0) {
                    Function targetFn = fnStack.Pop();
                    if (checkStack.Pop()) {
                        String fn = targetFn.fn;
                        switch (GetOperatorObject(fn[0])) {
                            case FunctionOperator.ADDITION:
                                List<Function> aChildren = new();
                                foreach (Function child in targetFn.children) {
                                    String childFn = child.fn;
                                    switch (GetOperatorObject(childFn[0])) {
                                        case FunctionOperator.ADDITION:
                                        case FunctionOperator.PARENTHETICAL:
                                            aChildren.AddRange(child.children);
                                            break;
                                        case FunctionOperator.ZERO:
                                            break; // does nothing
                                        default:
                                            aChildren.Add(child);
                                            break;
                                    }
                                }
                                targetFn.AssignValues(targetFn.fn, aChildren);
                                break;
                            case FunctionOperator.SUBTRACTION:
                                if (targetFn.children[0].Equals(targetFn.children[1])) {
                                    targetFn.AssignValues("0", new());
                                }
                                break;
                            case FunctionOperator.MULTIPLICATION:
                                List<Function> mChildren = new();
                                bool isZero = false;
                                foreach (Function child in targetFn.children) {
                                    String childFn = child.fn;
                                    switch (GetOperatorObject(childFn[0])) {
                                        case FunctionOperator.MULTIPLICATION:
                                            mChildren.AddRange(child.children);
                                            break;
                                        case FunctionOperator.PARENTHETICAL:
                                            Function grandchild = child.children[0];
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
                                }
                                targetFn.AssignValues(targetFn.fn, mChildren);
                                break;
                            case FunctionOperator.PARENTHETICAL:
                                Function pChild = targetFn.children[0];
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
                                Function nChild = targetFn.children[0];
                                switch (GetOperatorObject(nChild.fn[0])) {
                                    case FunctionOperator.NEGATION:
                                        Function grandchild = nChild.children[0];
                                        targetFn.AssignValues(grandchild.fn, grandchild.children);
                                        break;
                                    case FunctionOperator.PARENTHETICAL:
                                        Function pGrandchild = nChild.children[0];
                                        if (pGrandchild.fn[0] == '-') {
                                            Function greatGrandchild = pGrandchild.children[0];
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
                        if (simplifyAllLayers) {
                            foreach (Function child in targetFn.children) {
                                fnStack.Push(child);
                                checkStack.Push(false);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Assigns specific fields to this <c>Function</c>.
            /// </summary>
            /// <param name="fn">
            /// The new <c>Function</c> classification.
            /// </param>
            /// <param name="children">
            /// The new list of child <c>Function</c> objects.
            /// </param>
            private void AssignValues(String fn, List<Function> children) {
                this.fn = fn;
                this.children.Clear();
                this.children.AddRange(children);
            }

            /// <summary>
            /// Determines whether this <c>Function</c> is a constant.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this <c>Function</c> is a integer constant, else 
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
            /// Determines whether this <c>Function</c> is a variable.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this <c>Function</c> is a variable, else 
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
            /// Makes a copy of this <c>Function</c>.
            /// </summary>
            /// <returns>
            /// The copy.
            /// </returns>
            public Function Copy() {
                Function copy = new();
                Stack<Function> thisStack = new();
                Stack<Function> copyStack = new();
                thisStack.Push(this);
                copyStack.Push(copy);
                while (thisStack.Count > 0) {
                    Function targetThis = thisStack.Pop();
                    Function targetCopy = copyStack.Pop();
                    targetCopy.fn = targetThis.fn;
                    foreach (Function child in targetThis.children) {
                        thisStack.Push(child);
                        Function nextCopy = new();
                        copyStack.Push(nextCopy);
                        targetCopy.AddChild(nextCopy);
                    }
                }
                return copy;
            }

            /// <summary>
            /// Substitutes a specified variable in this <c>Function</c>
            /// with a different <c>Function</c>.
            /// </summary>
            /// <param name="var">
            /// The target variable.
            /// </param>
            /// <param name="fn">
            /// The replacement <c>Function</c>.
            /// </param>
            public void Substitute(Function var, Function fn) {
                if (!var.IsVariable()) {
                    throw new ArgumentException("Function must be argument.");
                }
                String varFn = var.fn;
                Stack<Function> thisStack = new();
                thisStack.Push(this);
                while (thisStack.Count > 0) {
                    Function target = thisStack.Pop();
                    if (target.fn.Equals(varFn)) {
                        target.AssignValues("(", new() { fn });
                        target.Simplify(false);
                    } else {
                        foreach (Function child in target.children) {
                            thisStack.Push(child);
                        }
                    }
                }
            }

            /// <summary>
            /// Adds two <c>Function</c> objects.
            /// </summary>
            /// <param name="addend">
            /// The subtrahend <c>Function</c>.
            /// </param>
            /// <returns>
            /// <c>this + subtrahend</c>
            /// </returns>
            public Function Add(Function addend) {
                Function sum = new();
                List<Function> children = new() {Copy(), addend.Copy()};
                sum.AssignValues("+", children);
                sum.Simplify(false);
                return sum;
            }

            /// <summary>
            /// Adds two <c>Function</c> objects.
            /// </summary>
            /// <param name="subtrahend">
            /// The subtrahend <c>Function</c>.
            /// </param>
            /// <returns>
            /// <c>this + subtrahend</c>
            /// </returns>
            public Function Subtract(Function subtrahend) {
                Function newSub = subtrahend.Copy();
                int compare = CompareOperators(FunctionOperator.SUBTRACTION, GetOperatorObject(newSub.fn[0]));
                if (compare >= 0) {
                    InsertParentheticalExpression(newSub);
                }
                Function difference = new();
                difference.AssignValues("-", new() {Copy(), newSub});
                difference.Simplify(false);
                return difference;
            }

            /// <summary>
            /// Multiplies two <c>Function</c> objects.
            /// </summary>
            /// <param name="multiplicand">
            /// The multiplicand <c>Function</c>.
            /// </param>
            /// <returns>
            /// <c>this * multiplicand</c>
            /// </returns>
            public Function Multiply(Function multiplicand) {
                Function thisCopy = Copy();
                Function multCopy = multiplicand.Copy();
                int compareThis = CompareOperators(FunctionOperator.MULTIPLICATION, GetOperatorObject(thisCopy.fn[0]));
                int compareMult = CompareOperators(FunctionOperator.MULTIPLICATION, GetOperatorObject(multCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                if (compareMult > 0) {
                    InsertParentheticalExpression(multCopy);
                }
                Function product = new();
                product.AssignValues("*", new() { thisCopy, multCopy});
                product.Simplify(false);
                return product;
            }

            /// <summary>
            /// Divides this <c>Function</c> by another specified <c>Function</c>.
            /// </summary>
            /// <param name="divisor">
            /// The divisor <c>Function</c>.
            /// </param>
            /// <returns>
            /// <c>this / divisor</c>
            /// </returns>
            public Function Divide(Function divisor) {
                Function thisCopy = Copy();
                Function diviCopy = divisor.Copy();
                int compareThis = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(thisCopy.fn[0]));
                int compareDivi = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(diviCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                if (compareDivi > 0) {
                    InsertParentheticalExpression(diviCopy);
                }
                Function quotient = new();
                quotient.AssignValues("/", new() { thisCopy, diviCopy });
                quotient.Simplify(false);
                return quotient;
            }

            /// <summary>
            /// Negates this <c>Function</c>.
            /// </summary>
            /// <returns>
            /// <c>-this</c>
            /// </returns>
            public Function Negate() {
                Function thisCopy = Copy();
                int compareThis = CompareOperators(FunctionOperator.DIVISION, GetOperatorObject(thisCopy.fn[0]));
                if (compareThis > 0) {
                    InsertParentheticalExpression(thisCopy);
                }
                Function negation = new();
                negation.AssignValues("!", new() { thisCopy });
                negation.Simplify(false);
                return negation;
            }

            /// <summary>
            /// Packages this <c>Function</c> inside a parenthetical
            /// <c>Function</c>.
            /// </summary>
            /// <param name="fn">
            /// The <c>Function</c> to be packaged.
            /// </param>
            private static void InsertParentheticalExpression(Function fn) {
                String fnStr = fn.fn;
                List<Function> children = new();
                children.AddRange(fn.children);
                Function replacement = new();
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
                    _ => 8
                };
            }

            /// <summary>
            /// Converts this <c>Function</c> to a printable format.
            /// </summary>
            /// <returns>
            /// This <c>Function</c> as a <c>String</c>.
            /// </returns>
            public override string ToString() {
                String fn = this.fn;
                char op = fn[0];
                switch ((FunctionOperator) op) {
                    case FunctionOperator.ADDITION:
                    case FunctionOperator.MULTIPLICATION:
                        StringBuilder builder = new();
                        char delimiter = ' ';
                        foreach (Function child in this.children) {
                            builder.Append(delimiter).Append(child);
                            delimiter = op;
                        }
                        return builder.ToString();
                    case FunctionOperator.SUBTRACTION:
                    case FunctionOperator.DIVISION:
                        return this.children[0].ToString() + op + this.children[1];
                    case FunctionOperator.PARENTHETICAL:
                        return "(" + this.children[0] + ')';
                    case FunctionOperator.DIFFERENTIAL:
                        return this.children[0].fn + "'";
                    case FunctionOperator.NEGATION:
                        return "-" + this.children[0];
                }
                return fn;  // If this statement is reached, this Function
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
                ZERO                        = 49, //    0
                ONE                         = 50  //    1
            }
        }
    }
}
