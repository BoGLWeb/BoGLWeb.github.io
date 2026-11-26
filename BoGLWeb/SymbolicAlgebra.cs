using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        /// <summary>
        /// Immutable product of symbolic variable powers (e.g. R_6^2 * J_10).
        /// </summary>
        internal sealed class Monomial : IEquatable<Monomial> {
            public static readonly Monomial One = new(new SortedDictionary<string, int>(StringComparer.Ordinal));

            public SortedDictionary<string, int> Powers { get; }
            private readonly string canonicalKey;

            public Monomial(SortedDictionary<string, int> powers) {
                SortedDictionary<string, int> clean = new(StringComparer.Ordinal);
                StringBuilder sb = new();
                foreach (var kv in powers) {
                    if (kv.Value > 0) {
                        clean[kv.Key] = kv.Value;
                        sb.Append(kv.Key).Append('^').Append(kv.Value).Append('*');
                    }
                }
                Powers = clean;
                canonicalKey = sb.ToString();
            }

            public static Monomial FromVar(string name) {
                return new Monomial(new SortedDictionary<string, int>(StringComparer.Ordinal) { { name, 1 } });
            }

            public bool IsOne => Powers.Count == 0;

            public Monomial Multiply(Monomial other) {
                if (IsOne) return other;
                if (other.IsOne) return this;
                SortedDictionary<string, int> result = new(Powers, StringComparer.Ordinal);
                foreach (var kv in other.Powers) {
                    result[kv.Key] = result.GetValueOrDefault(kv.Key) + kv.Value;
                }
                return new Monomial(result);
            }

            public Monomial Gcd(Monomial other) {
                if (IsOne || other.IsOne) return One;
                SortedDictionary<string, int> result = new(StringComparer.Ordinal);
                foreach (var kv in Powers) {
                    if (other.Powers.TryGetValue(kv.Key, out int exp)) {
                        int minExp = Math.Min(kv.Value, exp);
                        if (minExp > 0) result[kv.Key] = minExp;
                    }
                }
                return new Monomial(result);
            }

            public Monomial DivideExact(Monomial divisor) {
                if (divisor.IsOne) return this;
                SortedDictionary<string, int> result = new(Powers, StringComparer.Ordinal);
                foreach (var kv in divisor.Powers) {
                    int diff = result.GetValueOrDefault(kv.Key) - kv.Value;
                    if (diff < 0) {
                        throw new InvalidOperationException("Monomial division is not exact.");
                    }
                    if (diff == 0) result.Remove(kv.Key);
                    else result[kv.Key] = diff;
                }
                return new Monomial(result);
            }

            public string ToLatex() {
                if (IsOne) return "1";
                StringBuilder sb = new();
                bool first = true;
                foreach (var kv in Powers) {
                    if (!first) sb.Append(' ');
                    first = false;
                    sb.Append(Expression.FormatLatexIdentifier(kv.Key));
                    if (kv.Value > 1) {
                        sb.Append("^{").Append(kv.Value).Append('}');
                    }
                }
                return sb.ToString();
            }

            public bool Equals(Monomial? other) => other is not null && canonicalKey == other.canonicalKey;
            public override bool Equals(object? obj) => obj is Monomial m && Equals(m);
            public override int GetHashCode() => canonicalKey.GetHashCode();
            public override string ToString() => IsOne ? "1" : canonicalKey.TrimEnd('*');
        }

        /// <summary>
        /// Multivariate polynomial over exact 64-bit integer coefficients.
        /// </summary>
        internal sealed class SymbolicPoly : IEquatable<SymbolicPoly> {
            public static readonly SymbolicPoly Zero = new(new Dictionary<Monomial, long>());
            public static readonly SymbolicPoly One = FromInt(1);

            private readonly Dictionary<Monomial, long> terms;

            private SymbolicPoly(Dictionary<Monomial, long> terms) {
                this.terms = new Dictionary<Monomial, long>();
                foreach (var kv in terms) {
                    if (kv.Value != 0) {
                        this.terms[kv.Key] = kv.Value;
                    }
                }
            }

            public static SymbolicPoly FromInt(long value) {
                if (value == 0) return Zero;
                return new SymbolicPoly(new Dictionary<Monomial, long> { { Monomial.One, value } });
            }

            public static SymbolicPoly FromVar(string name) {
                return new SymbolicPoly(new Dictionary<Monomial, long> { { Monomial.FromVar(name), 1 } });
            }

            public bool IsZero => terms.Count == 0;
            public bool IsOne => terms.Count == 1 && terms.TryGetValue(Monomial.One, out long v) && v == 1;
            public bool IsMinusOne => terms.Count == 1 && terms.TryGetValue(Monomial.One, out long v) && v == -1;
            public bool IsSingleTerm => terms.Count == 1;
            public bool IsConstant => terms.Count == 0 || (terms.Count == 1 && terms.ContainsKey(Monomial.One));

            public long LeadingCoefficient {
                get {
                    if (IsZero) return 0;
                    foreach (var kv in GetOrderedTerms()) {
                        return kv.Value;
                    }
                    return 0;
                }
            }

            public bool IsNegativeLeading => LeadingCoefficient < 0;

            public SymbolicPoly Negate() {
                if (IsZero) return Zero;
                Dictionary<Monomial, long> res = new();
                foreach (var kv in terms) res[kv.Key] = -kv.Value;
                return new SymbolicPoly(res);
            }

            public SymbolicPoly Add(SymbolicPoly other) {
                if (IsZero) return other;
                if (other.IsZero) return this;
                Dictionary<Monomial, long> res = new(terms);
                foreach (var kv in other.terms) {
                    long sum = checked(res.GetValueOrDefault(kv.Key) + kv.Value);
                    if (sum == 0) res.Remove(kv.Key);
                    else res[kv.Key] = sum;
                }
                return new SymbolicPoly(res);
            }

            public SymbolicPoly Subtract(SymbolicPoly other) => Add(other.Negate());

            public SymbolicPoly Multiply(SymbolicPoly other) {
                if (IsZero || other.IsZero) return Zero;
                if (IsOne) return other;
                if (other.IsOne) return this;
                Dictionary<Monomial, long> res = new();
                foreach (var a in terms) {
                    foreach (var b in other.terms) {
                        Monomial m = a.Key.Multiply(b.Key);
                        long prod = checked(a.Value * b.Value);
                        long sum = checked(res.GetValueOrDefault(m) + prod);
                        if (sum == 0) res.Remove(m);
                        else res[m] = sum;
                    }
                }
                return new SymbolicPoly(res);
            }

            public (long IntGcd, Monomial MonGcd) ContentGcd() {
                if (IsZero) return (0, Monomial.One);
                long g = 0;
                Monomial? mGcd = null;
                foreach (var kv in terms) {
                    long absVal = Math.Abs(kv.Value);
                    g = g == 0 ? absVal : IntGcd(g, absVal);
                    mGcd = mGcd is null ? kv.Key : mGcd.Gcd(kv.Key);
                }
                return (g, mGcd ?? Monomial.One);
            }

            public SymbolicPoly DivideExact(long intDivisor, Monomial monDivisor) {
                if (IsZero) return Zero;
                Dictionary<Monomial, long> res = new();
                foreach (var kv in terms) {
                    res[kv.Key.DivideExact(monDivisor)] = kv.Value / intDivisor;
                }
                return new SymbolicPoly(res);
            }

            public static long IntGcd(long a, long b) {
                a = Math.Abs(a);
                b = Math.Abs(b);
                while (b != 0) {
                    long t = a % b;
                    a = b;
                    b = t;
                }
                return a == 0 ? 1 : a;
            }

            private IEnumerable<KeyValuePair<Monomial, long>> GetOrderedTerms() {
                return terms
                    .OrderByDescending(kv => kv.Key.IsOne ? 1 : 0)
                    .ThenBy(kv => kv.Value < 0 ? 1 : 0)
                    .ThenBy(kv => kv.Key.ToString(), StringComparer.Ordinal);
            }

            public string ToLatex() {
                if (IsZero) return "0";
                StringBuilder sb = new();
                bool first = true;
                foreach (var kv in GetOrderedTerms()) {
                    long coeff = kv.Value;
                    bool neg = coeff < 0;
                    long mag = Math.Abs(coeff);
                    if (!first) {
                        sb.Append(neg ? " - " : " + ");
                    } else if (neg) {
                        sb.Append('-');
                    }
                    first = false;

                    if (kv.Key.IsOne) {
                        sb.Append(mag);
                    } else {
                        if (mag != 1) {
                            sb.Append(mag).Append(' ');
                        }
                        sb.Append(kv.Key.ToLatex());
                    }
                }
                return sb.ToString();
            }

            public bool Equals(SymbolicPoly? other) {
                if (other is null || terms.Count != other.terms.Count) return false;
                foreach (var kv in terms) {
                    if (!other.terms.TryGetValue(kv.Key, out long v) || v != kv.Value) return false;
                }
                return true;
            }

            public override bool Equals(object? obj) => obj is SymbolicPoly p && Equals(p);
            public override int GetHashCode() => terms.Count;
        }

        /// <summary>
        /// Exact rational function of parameter symbols (Num / Den), normalized by content GCD.
        /// </summary>
        internal sealed class SymbolicRational : IEquatable<SymbolicRational> {
            public static readonly SymbolicRational Zero = new(SymbolicPoly.Zero, SymbolicPoly.One, true);
            public static readonly SymbolicRational One = new(SymbolicPoly.One, SymbolicPoly.One, true);

            public SymbolicPoly Num { get; }
            public SymbolicPoly Den { get; }

            private SymbolicRational(SymbolicPoly num, SymbolicPoly den, bool normalized) {
                if (normalized) {
                    Num = num;
                    Den = den;
                    return;
                }
                if (den.IsZero) {
                    throw new DivideByZeroException("Symbolic denominator became zero.");
                }
                if (num.IsZero) {
                    Num = SymbolicPoly.Zero;
                    Den = SymbolicPoly.One;
                    return;
                }
                // Keep the denominator's leading coefficient positive so signs live on the numerator.
                if (den.IsNegativeLeading) {
                    num = num.Negate();
                    den = den.Negate();
                }
                // Divide out common integer and monomial content between numerator and denominator.
                var (ng, nm) = num.ContentGcd();
                var (dg, dm) = den.ContentGcd();
                long g = SymbolicPoly.IntGcd(ng, dg);
                Monomial m = nm.Gcd(dm);
                num = num.DivideExact(g, m);
                den = den.DivideExact(g, m);

                // Cancel identical primitive polynomial factors (e.g. (1+a)/(1+a) or c*(1+a)/(1+a)).
                var (ng2, nm2) = num.ContentGcd();
                var (dg2, dm2) = den.ContentGcd();
                SymbolicPoly primNum = num.DivideExact(ng2, nm2);
                SymbolicPoly primDen = den.DivideExact(dg2, dm2);
                if (primNum.Equals(primDen)) {
                    num = SymbolicPoly.FromInt(ng2).Multiply(new SymbolicPolyProxy(nm2).Poly);
                    den = SymbolicPoly.FromInt(dg2).Multiply(new SymbolicPolyProxy(dm2).Poly);
                } else if (primNum.Equals(primDen.Negate())) {
                    num = SymbolicPoly.FromInt(-ng2).Multiply(new SymbolicPolyProxy(nm2).Poly);
                    den = SymbolicPoly.FromInt(dg2).Multiply(new SymbolicPolyProxy(dm2).Poly);
                }

                Num = num;
                Den = den;
            }

            private readonly struct SymbolicPolyProxy {
                public SymbolicPoly Poly { get; }
                public SymbolicPolyProxy(Monomial m) {
                    Poly = SymbolicPoly.One;
                    foreach (var kv in m.Powers) {
                        var factor = SymbolicPoly.FromVar(kv.Key);
                        for (int i = 0; i < kv.Value; i++) {
                            Poly = Poly.Multiply(factor);
                        }
                    }
                }
            }

            public static SymbolicRational FromInt(long v) =>
                v == 0 ? Zero : new SymbolicRational(SymbolicPoly.FromInt(v), SymbolicPoly.One, true);

            public static SymbolicRational FromVar(string name) =>
                new(SymbolicPoly.FromVar(name), SymbolicPoly.One, true);

            public static SymbolicRational FromRatio(SymbolicPoly num, SymbolicPoly den) =>
                new(num, den, false);

            public bool IsZero => Num.IsZero;
            public bool IsOne => Num.IsOne && Den.IsOne;
            public bool IsMinusOne => Num.IsMinusOne && Den.IsOne;
            public bool IsNegative => !IsZero && Num.IsNegativeLeading;

            public SymbolicRational Negate() =>
                IsZero ? Zero : new SymbolicRational(Num.Negate(), Den, true);

            public SymbolicRational Add(SymbolicRational other) {
                if (IsZero) return other;
                if (other.IsZero) return this;
                if (Den.Equals(other.Den)) {
                    return new SymbolicRational(Num.Add(other.Num), Den, false);
                }
                return new SymbolicRational(Num.Multiply(other.Den).Add(other.Num.Multiply(Den)), Den.Multiply(other.Den), false);
            }

            public SymbolicRational Subtract(SymbolicRational other) => Add(other.Negate());

            public SymbolicRational Multiply(SymbolicRational other) {
                if (IsZero || other.IsZero) return Zero;
                if (IsOne) return other;
                if (other.IsOne) return this;
                return new SymbolicRational(Num.Multiply(other.Num), Den.Multiply(other.Den), false);
            }

            public SymbolicRational Divide(SymbolicRational divisor) {
                if (divisor.IsZero) throw new DivideByZeroException("Division by zero in symbolic expression.");
                if (IsZero) return Zero;
                if (divisor.IsOne) return this;
                return new SymbolicRational(Num.Multiply(divisor.Den), Den.Multiply(divisor.Num), false);
            }

            public string ToLatex() {
                if (IsZero) return "0";
                if (Den.IsOne) return Num.ToLatex();
                if (Num.IsNegativeLeading && Num.IsSingleTerm) {
                    return "-\\frac{" + Num.Negate().ToLatex() + "}{" + Den.ToLatex() + "}";
                }
                return "\\frac{" + Num.ToLatex() + "}{" + Den.ToLatex() + "}";
            }

            public string ToTermLatex(string dynamicVarLatex, bool includeSign) {
                bool neg = IsNegative;
                SymbolicRational absCoeff = neg ? Negate() : this;
                string prefix = includeSign ? (neg ? "-" : "") : "";

                if (absCoeff.IsOne) {
                    return prefix + dynamicVarLatex;
                }
                if (absCoeff.Den.IsOne) {
                    string coeffLatex = absCoeff.Num.IsSingleTerm
                        ? absCoeff.Num.ToLatex()
                        : "(" + absCoeff.Num.ToLatex() + ")";
                    return prefix + coeffLatex + " " + dynamicVarLatex;
                }
                if (absCoeff.Num.IsOne) {
                    return prefix + "\\frac{" + dynamicVarLatex + "}{" + absCoeff.Den.ToLatex() + "}";
                }
                return prefix + "\\frac{" + absCoeff.Num.ToLatex() + "}{" + absCoeff.Den.ToLatex() + "} " + dynamicVarLatex;
            }

            public bool Equals(SymbolicRational? other) =>
                other is not null && Num.Equals(other.Num) && Den.Equals(other.Den);
            public override bool Equals(object? obj) => obj is SymbolicRational r && Equals(r);
            public override int GetHashCode() => HashCode.Combine(Num, Den);
        }

        /// <summary>
        /// Linear combination of dynamic variables (states, derivatives, sources) with SymbolicRational coefficients.
        /// </summary>
        internal sealed class LinearForm {
            public static readonly LinearForm Zero = new(new Dictionary<string, SymbolicRational>(StringComparer.Ordinal));

            private readonly Dictionary<string, SymbolicRational> terms;

            private LinearForm(Dictionary<string, SymbolicRational> terms) {
                this.terms = new Dictionary<string, SymbolicRational>(StringComparer.Ordinal);
                foreach (var kv in terms) {
                    if (!kv.Value.IsZero) {
                        this.terms[kv.Key] = kv.Value;
                    }
                }
            }

            public static LinearForm FromVar(string varKey) =>
                new(new Dictionary<string, SymbolicRational>(StringComparer.Ordinal) { { varKey, SymbolicRational.One } });

            public static LinearForm FromScaledVar(string varKey, SymbolicRational coeff) =>
                coeff.IsZero ? Zero : new(new Dictionary<string, SymbolicRational>(StringComparer.Ordinal) { { varKey, coeff } });

            public IReadOnlyDictionary<string, SymbolicRational> Terms => terms;
            public bool IsZero => terms.Count == 0;

            public SymbolicRational GetCoeff(string varKey) =>
                terms.GetValueOrDefault(varKey, SymbolicRational.Zero);

            public LinearForm Negate() {
                if (IsZero) return Zero;
                Dictionary<string, SymbolicRational> res = new(StringComparer.Ordinal);
                foreach (var kv in terms) res[kv.Key] = kv.Value.Negate();
                return new LinearForm(res);
            }

            public LinearForm Add(LinearForm other) {
                if (IsZero) return other;
                if (other.IsZero) return this;
                Dictionary<string, SymbolicRational> res = new(terms, StringComparer.Ordinal);
                foreach (var kv in other.terms) {
                    SymbolicRational sum = res.GetValueOrDefault(kv.Key, SymbolicRational.Zero).Add(kv.Value);
                    if (sum.IsZero) res.Remove(kv.Key);
                    else res[kv.Key] = sum;
                }
                return new LinearForm(res);
            }

            public LinearForm Subtract(LinearForm other) => Add(other.Negate());

            public LinearForm Scale(SymbolicRational factor) {
                if (IsZero || factor.IsZero) return Zero;
                if (factor.IsOne) return this;
                Dictionary<string, SymbolicRational> res = new(StringComparer.Ordinal);
                foreach (var kv in terms) res[kv.Key] = kv.Value.Multiply(factor);
                return new LinearForm(res);
            }

            public LinearForm DivideBy(SymbolicRational divisor) => Scale(SymbolicRational.One.Divide(divisor));

            public LinearForm Differentiate() {
                if (IsZero) return Zero;
                Dictionary<string, SymbolicRational> res = new(StringComparer.Ordinal);
                foreach (var kv in terms) {
                    string derivKey = kv.Key + "'";
                    res[derivKey] = res.GetValueOrDefault(derivKey, SymbolicRational.Zero).Add(kv.Value);
                }
                return new LinearForm(res);
            }

            public LinearForm Substitute(string varKey, LinearForm replacement) {
                if (!terms.TryGetValue(varKey, out SymbolicRational? coeff)) {
                    return this;
                }
                Dictionary<string, SymbolicRational> baseTerms = new(terms, StringComparer.Ordinal);
                baseTerms.Remove(varKey);
                return new LinearForm(baseTerms).Add(replacement.Scale(coeff));
            }

            public LinearForm WithoutVars(Func<string, bool> predicate) {
                Dictionary<string, SymbolicRational> res = new(StringComparer.Ordinal);
                foreach (var kv in terms) {
                    if (!predicate(kv.Key)) {
                        res[kv.Key] = kv.Value;
                    }
                }
                return new LinearForm(res);
            }

            public string ToLatex(Func<string, string> formatVarKey) {
                if (IsZero) return "0";
                var ordered = terms
                    .OrderBy(kv => kv.Value.IsNegative ? 1 : 0)
                    .ThenBy(kv => kv.Key.EndsWith("'") ? 1 : 0)
                    .ThenBy(kv => kv.Key, StringComparer.Ordinal);

                StringBuilder sb = new();
                bool first = true;
                foreach (var kv in ordered) {
                    bool neg = kv.Value.IsNegative;
                    SymbolicRational absCoeff = neg ? kv.Value.Negate() : kv.Value;
                    if (!first) {
                        sb.Append(neg ? " - " : " + ");
                    } else if (neg) {
                        sb.Append('-');
                    }
                    first = false;
                    sb.Append(absCoeff.ToTermLatex(formatVarKey(kv.Key), includeSign: false));
                }
                return sb.ToString();
            }

            /// <summary>
            /// Solves M * x = b via symbolic Gaussian elimination with partial pivoting.
            /// </summary>
            public static LinearForm[] SolveLinearSystem(SymbolicRational[,] matrix, LinearForm[] rhs) {
                int n = rhs.Length;
                SymbolicRational[,] a = new SymbolicRational[n, n];
                LinearForm[] b = new LinearForm[n];
                for (int i = 0; i < n; i++) {
                    b[i] = rhs[i];
                    for (int j = 0; j < n; j++) {
                        a[i, j] = matrix[i, j];
                    }
                }

                for (int col = 0; col < n; col++) {
                    int pivot = -1;
                    for (int row = col; row < n; row++) {
                        if (!a[row, col].IsZero) {
                            pivot = row;
                            break;
                        }
                    }
                    if (pivot == -1) {
                        throw new InvalidOperationException("Singular algebraic/derivative causality system cannot be uniquely inverted.");
                    }
                    if (pivot != col) {
                        for (int j = col; j < n; j++) {
                            (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                        }
                        (b[col], b[pivot]) = (b[pivot], b[col]);
                    }

                    SymbolicRational diag = a[col, col];
                    if (!diag.IsOne) {
                        for (int j = col; j < n; j++) {
                            a[col, j] = a[col, j].Divide(diag);
                        }
                        b[col] = b[col].DivideBy(diag);
                    }

                    for (int row = 0; row < n; row++) {
                        if (row == col || a[row, col].IsZero) continue;
                        SymbolicRational factor = a[row, col];
                        for (int j = col; j < n; j++) {
                            a[row, j] = a[row, j].Subtract(factor.Multiply(a[col, j]));
                        }
                        b[row] = b[row].Subtract(b[col].Scale(factor));
                    }
                }
                return b;
            }
        }
    }
}
