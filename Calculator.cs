using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Calculator
{
    /// <summary>
    /// Calculator class which handles evaluating arithmetic expressions.
    /// Supports the following:  Multiplication, Division, Addition, Subtraction,
    /// X ^ Y, Remainder (Modulo), Square, Cube, Sine, Cosine, Tangent and UnaryMinus.
    /// </summary>
    class Calculator
    {
        #region Expression Delegates
        Expression<Func<double, double, double>> Multiplication = (x, y) => x * y;
        Expression<Func<double, double, double>> Division = (x, y) => x / y;
        Expression<Func<double, double, double>> Addition = (x, y) => x + y;
        Expression<Func<double, double, double>> Subtraction = (x, y) => x - y;
        Expression<Func<double, double, double>> Caret = (x, y) => Math.Pow(x, y);
        Expression<Func<double, double, double>> Remainder = (x, y) => x % y;
        Expression<Func<double, double>> Square = (x) => Math.Pow(x, 2);
        Expression<Func<double, double>> Cubed = (x) => Math.Pow(x, 3);
        Expression<Func<double, double>> Sine = (x) => Math.Sin(Math.PI * x /180);
        Expression<Func<double, double>> Cosine = (x) => Math.Cos(Math.PI * x / 180);
        Expression<Func<double, double>> Tangent = (x) => Math.Tan(Math.PI * x / 180);
        Expression<Func<double, double>> UnaryMinus = (x) => x * -1;
        #endregion

        #region ReadOnly
        private readonly List<TokenType> OperatorTypes = new List<TokenType>()
            { TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide, TokenType.Remainder};

        private readonly List<TokenType> FunctionTypes = new List<TokenType>()
            { TokenType.Squared, TokenType.Cubed, TokenType.Sine, TokenType.Cosine, TokenType.Tangent, TokenType.UnaryMinus, TokenType.Caret};
        #endregion

        #region Properties
        /// Properties include a List of Tokens that are parsed from the inputted expression string, 
        /// as well as a Queue of Tokens that represent the expression in Postfix format (Reverse Polish Notation).
        public Queue<Token> Postfix { get; set; }
        public List<Token> Tokens { get; set; }
        #endregion 

        #region Constructors
        public Calculator()
        {
            Postfix = new Queue<Token>();
            Tokens = new List<Token>();
        }
        #endregion

        #region Calculate
        /// <summary>
        /// Run the Calculator.  This parser uses ascii codes for the multiplication and division symbol.  
        /// Decimal Ascii Code for Multiplication Symbol is 215.
        /// Decimal Ascii Code for Division Symbol is 247.
        /// </summary>
        /// <param name="expression">expression string</param>
        /// <returns>the answer in a double</returns>
        /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
        /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
        public double Calculate(string expression)
        {
            try
            {
                //get tokens
                Tokens = new List<Token>(GetTokens(expression));

                //Shunting Yard Algorithm
                Postfix = ShuntingYardAlgorithm(Tokens);

                //Evaluate Reverse Polish Notation and return result
                return ReversePolishEvaluation(Postfix);
            }
            catch (DivideByZeroException e)
            {
                Console.WriteLine(e.Message);
                Debug.WriteLine(e.Message);
                throw new DivideByZeroException("Cannot Divide by Zero.");
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Format");
                Debug.WriteLine("Invalid Format");
                throw new FormatException("Invalid Format");
            }
        }
        #endregion

        #region Shunting Yard Algorithm
        /// <summary>
        /// Runs a modified Shunting Yard Algorithm
        /// to convert the tokens into postfix (RPN) notation.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
        /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
        private Queue<Token> ShuntingYardAlgorithm(List<Token> tokens)
        {
            Queue<Token> Postfix = new Queue<Token>();
            Stack<Token> operatorStack = new Stack<Token>();

            foreach (var token in tokens)
            {
                //if token is a number or exponent
                if (token.Type == TokenType.Number || token.Type == TokenType.Exponent)
                    Postfix.Enqueue(token);

                //if the token is a function
                else if (FunctionTypes.Contains(token.Type))
                    operatorStack.Push(token);

                //if the token is an operator
                else if (OperatorTypes.Contains(token.Type))
                {
                    //if the operator currently on top of the stack is of greater precedence than the
                    //current token then POP the operator off the stack and on the queue
                    while (operatorStack.Count > 0 && operatorStack.Peek().Precedence >= token.Precedence)
                        Postfix.Enqueue(operatorStack.Pop());

                    //push current token on the stack
                    operatorStack.Push(token);
                }

                //if the token is a left parenthesis
                else if (token.Type == TokenType.LeftParenthesis)
                    operatorStack.Push(token);

                //if the token is a right parenthesis
                else if (token.Type == TokenType.RightParenthesis)
                {
                    //while the operator on the top of the stack is not a left parenthesis then
                    //POP the operator off the stack and on the queue
                    while (operatorStack.Count > 0 && operatorStack.Peek().Type != TokenType.LeftParenthesis)               
                       Postfix.Enqueue(operatorStack.Pop());
                    
                    //remove left parenthesis
                    operatorStack.Pop();

                    //check if a function or unary minus was to the left of the left parenthesis
                    while (operatorStack.Count > 0 && FunctionTypes.Contains(operatorStack.Peek().Type))
                        Postfix.Enqueue(operatorStack.Pop());
                }
            }

            //POP the remaining operators onto the postfix queue
            while (operatorStack.Count > 0)
                Postfix.Enqueue(operatorStack.Pop());

            return Postfix;
        }
        #endregion

        #region Reverse Polish Evaluation
        /// <summary>
        /// Evaluates the expression in RPN and returns the calculated result.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
        /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
        private double ReversePolishEvaluation(Queue<Token> output)
        {
            Stack<Token> stack = new Stack<Token>();

            foreach (var item in output)
            {
                //if the token is a number or exponent
                if (item.Type == TokenType.Number || item.Type == TokenType.Exponent)
                    stack.Push(item);

                //if the token is an operator
                else if (OperatorTypes.Contains(item.Type))
                {
                    Token TokenRight = stack.Pop();
                    Token TokenLeft = stack.Pop();

                    switch (item.Type)
                    {
                        case TokenType.Plus:
                            stack.Push(new Token($"{Addition.Compile()(double.Parse(TokenLeft.Value), double.Parse(TokenRight.Value))}", TokenType.Number));
                            break;
                        case TokenType.Minus:
                            stack.Push(new Token($"{Subtraction.Compile()(double.Parse(TokenLeft.Value), double.Parse(TokenRight.Value))}", TokenType.Number));
                            break;
                        case TokenType.Multiply:
                            stack.Push(new Token($"{Multiplication.Compile()(double.Parse(TokenLeft.Value), double.Parse(TokenRight.Value))}", TokenType.Number));
                            break;
                        case TokenType.Divide:
                            if (double.Parse(TokenRight.Value) == 0)
                                throw new DivideByZeroException("You cannot divide by zero");

                            stack.Push(new Token($"{Division.Compile()(double.Parse(TokenLeft.Value), double.Parse(TokenRight.Value))}", TokenType.Number));
                            break;                        
                        case TokenType.Remainder:
                            stack.Push(new Token($"{Remainder.Compile()(double.Parse(TokenLeft.Value), double.Parse(TokenRight.Value))}", TokenType.Number));
                            break;
                    }
                }
                //if the token is a function
                else if (FunctionTypes.Contains(item.Type))
                {
                    Token topToken = stack.Pop();

                    switch (item.Type)
                    {
                        case TokenType.Caret:
                            stack.Push(new Token($"{Caret.Compile()(double.Parse(stack.Pop().Value), double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.Squared:
                            stack.Push(new Token($"{Square.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.Cubed:
                            stack.Push(new Token($"{Cubed.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.UnaryMinus:
                            stack.Push(new Token($"{UnaryMinus.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.Sine:
                            stack.Push(new Token($"{Sine.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.Cosine:
                            stack.Push(new Token($"{Cosine.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                        case TokenType.Tangent:
                            stack.Push(new Token($"{Tangent.Compile()(double.Parse(topToken.Value))}", TokenType.Number));
                            break;
                    }
                }
            }

            //return the calculated result
            return double.Parse(stack.Pop().Value);
        }
        #endregion

        #region Expression Parser
        /// <summary>
        /// Parses the expression string and creates a list of tokens
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>List of Tokens</returns>
        /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/18/2019</created>
        /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/18/2019</changed>
        private List<Token> GetTokens(string expression)
        {
            List<Token> tokens = new List<Token>();

            int tracker = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                //if current character isn't a space
                if (expression[i] != ' ')
                {
                    //If char is a number
                    if (Char.IsDigit(expression[i]))
                    {
                        bool addToken = false;

                        //if this is the last char in the expression string then addToken
                        if (i == (expression.Length - 1))
                            addToken = true;

                        //if the next char isn't a number then addToken
                        else if (!Char.IsDigit(expression[i + 1]))
                            addToken = true;

                        if (addToken)
                        {
                            Token newToken;

                            if (tokens.Count > 0 && tokens.Last().Type == TokenType.Caret)
                                newToken = new Token(expression.Substring(tracker, (i - tracker) + 1), TokenType.Exponent);
                            else
                                newToken = new Token(expression.Substring(tracker, (i - tracker) + 1), TokenType.Number);

                            tokens.Add(newToken);
                            tracker = i + 1;
                        }

                        //if the next char is a number then do nothing
                    }
                    //ElseIf char is an addition
                    else if (expression[i] == '+')
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.Plus);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a subtraction or unary minus
                    else if (expression[i] == '-')
                    {
                        Token newToken = default;

                        if (tokens.Count == 0)
                            newToken = new Token(expression.Substring(i, 1), TokenType.UnaryMinus);
                        else if (OperatorTypes.Contains(tokens.Last().Type))
                            newToken = new Token(expression.Substring(i, 1), TokenType.UnaryMinus);
                        else
                            newToken = new Token(expression.Substring(i, 1), TokenType.Minus);

                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a multiplication
                    else if (215 == (int)expression[i])
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.Multiply);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a remainder (modulo)
                    else if (expression[i] == '%')
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.Remainder);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a division
                    else if (247 == (int)expression[i])
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.Divide);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a Caret
                    else if (expression[i] == '^')
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.Caret);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a Left Parenthesis
                    else if (expression[i] == '(')
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.LeftParenthesis);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a Right Parenthesis
                    else if (expression[i] == ')')
                    {
                        Token newToken = new Token(expression.Substring(i, 1), TokenType.RightParenthesis);
                        tokens.Add(newToken);
                        tracker = i + 1;
                    }
                    //ElseIf char is a Sine function
                    else if (expression.Substring(i, 3) == "sin")
                    {
                        Token newToken = new Token(expression.Substring(i, 3), TokenType.Sine);
                        tokens.Add(newToken);
                        i = i + 2;
                        tracker = i + 1;
                    }
                    //ElseIf char is a Cosine function
                    else if (expression.Substring(i, 3) == "cos")
                    {
                        Token newToken = new Token(expression.Substring(i, 3), TokenType.Cosine);
                        tokens.Add(newToken);
                        i = i + 2;
                        tracker = i + 1;
                    }
                    //ElseIf char is a Tangent function
                    else if (expression.Substring(i, 3) == "tan")
                    {
                        Token newToken = new Token(expression.Substring(i, 3), TokenType.Tangent);
                        tokens.Add(newToken);
                        i = i + 2;
                        tracker = i + 1;
                    }
                    //ElseIf char is a Square function
                    else if (expression.Substring(i, 3) == "sqr")
                    {
                        Token newToken = new Token(expression.Substring(i, 3), TokenType.Squared);
                        tokens.Add(newToken);
                        i = i + 2;
                        tracker = i + 1;
                    }
                    //ElseIf char is a Cubed function
                    else if (expression.Substring(i, 4) == "cube")
                    {
                        Token newToken = new Token(expression.Substring(i, 4), TokenType.Cubed);
                        tokens.Add(newToken);
                        i = i + 3;
                        tracker = i + 1;
                    }
                }
                else
                    tracker = i + 1;
            }

            return tokens;
        }
        #endregion

        #region Struct
        /// <summary>
        /// Struct that represent a single token in the parsed expression string
        /// </summary>
        internal struct Token
        {
            public TokenType Type { get; set; }
            public short Precedence { get; set; }
            public string Value { get; set; }

            /// <summary>
            /// Constructor for the Token Struct
            /// </summary>
            /// <param name="value">The Value of the Token</param>
            /// <param name="type">The Type of the Token (TokenType)</param>
            /// <returns></returns>
            /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
            /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
            public Token(string value, TokenType type)
            {
                Type = type;
                Value = value.Trim();
                Precedence = default;
                Precedence = GetPrecedence();
            }

            /// <summary>
            /// Display a Token's information
            /// </summary>
            /// <returns></returns>
            /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
            /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
            public override string ToString()
            {
                return $"Value: {Value},  Type: {Type.ToString()},  Precedence: {Precedence}";
            }

            /// <summary>
            /// Determine the precedence of an operator token
            /// </summary>
            /// <returns></returns>
            /// <created>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</created>
            /// <changed>Andrew Haselden, andrewhaselden@gmail.com ,4/20/2019</changed>
            private short GetPrecedence()
            {
                if (Type == TokenType.Multiply || Type == TokenType.Divide || Type == TokenType.Remainder)
                    return 3;
                else if (Type == TokenType.Plus || Type == TokenType.Minus)
                    return 2;
                else if (Type == TokenType.Caret)
                    return 4;
                else
                    return 0;
            }
        }
        #endregion

        #region Enum
        /// <summary>The Various Types of Tokens</summary>
        internal enum TokenType
        {
            None, Number, Plus, Minus, Multiply, Divide, Remainder, Caret, Exponent, Squared, Cubed, UnaryMinus, Sine, Cosine, Tangent, LeftParenthesis, RightParenthesis
        }
        #endregion
    }
}
