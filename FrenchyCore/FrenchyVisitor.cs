using Antlr4.Runtime.Misc;
using Frenchy.FrenchyCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class FrenchyVisitor : FrenchyBaseVisitor<object?>
{
    #region CONSTRUCTOR
    public FrenchyVisitor()
    {
       //Set const functions and variables
        Constants["Pi"] = 3.14f;
        Constants["MsgConsole"] = new Func<object?[], object?>(MsgConsole);
        Constants["Pause"] = new Func<object?>(Pause);
        Constants["Taille"] = new Func<string, int>(Size);
    }
    #endregion
    private Dictionary<string, object?> Variables { get; } = new();
    private Dictionary<string, object?> Constants { get; } = new();
    private Dictionary<string, object?> Temps { get; } = new();

    #region VISIT OVERRIDES
    public override object? VisitAssignment(FrenchyParser.AssignmentContext context)
    {
        var varName = context.IDENTIFIER().GetText();
        var value = Visit(context.expression());

        if (Constants.ContainsKey(varName))
            throw new Exception($"The variable {varName} already exists as a constant");

        Variables[varName] = value;

        return null;
    }
    public override object? VisitAssignmentTemp(FrenchyParser.AssignmentTempContext context)
    {
        var tempVarName = context.IDENTIFIER().GetText();
        var tempValue = Visit(context.expression());

        if (tempValue is int || tempValue is float)
            Temps[tempVarName] = tempValue;
        else if (tempValue is not int && tempValue is not float)
            throw new Exception($"Variable {tempVarName} n'est pas un int ou un float!");

        return tempVarName;
    }

    public override object? VisitAssignmentTempForeach(FrenchyParser.AssignmentTempForeachContext context)
    {
        var tempVarDataType = context.dataTypes().GetText();
        var tempVarName = context.IDENTIFIER().GetText();

        if (Temps.ContainsKey(tempVarName) || Constants.ContainsKey(tempVarName) || Variables.ContainsKey(tempVarName))
            throw new Exception($"La variable {tempVarName} existe déjà!");

        //object?[] datas = new object?[]
        //{
        //    tempVarDataType,
        //    tempVarName
        //};

        var datas = tempVarName;

        return datas;
    }

    public override object? VisitConstant(FrenchyParser.ConstantContext context)
    {
        if (context.INTEGER() is { } i)
            return int.Parse(i.GetText());

        if (context.FLOAT() is { } f)
            return float.Parse(f.GetText(), CultureInfo.InvariantCulture.NumberFormat);

        if (context.STRING() is { } s)
            return s.GetText()[1..^1];

        if (context.BOOL() is { } b)
            return b.GetText() == "true";

        if (context.NULL() is { })
            return null;

        throw new NotImplementedException();
    }
    public override object? VisitIdentifierExpression(FrenchyParser.IdentifierExpressionContext context)
    {
        var varName = context.IDENTIFIER().GetText();

        if (Variables.ContainsKey(varName))
            return Variables[varName];
        else if (Constants.ContainsKey(varName))
            return Constants[varName];
        else if (Temps.ContainsKey(varName))
            return Temps[varName];

        throw new Exception($"La variable {varName} n'est pas définie");
    }
    public override object? VisitList(FrenchyParser.ListContext context)
    {
        var type = context.dataTypes().GetText();
        bool isSameType = false;

        var listFinal = new List<object?>();

        if (context.listDatas() != null)
        {
            for (int j = 0; j < context.listDatas().constant().Length; j++)
            {
                isSameType = false;

                switch (type)
                {
                    case "INTEGER":
                        if (Visit(context.listDatas().constant()[j]) is int i)
                            isSameType = true;
                        break;
                    case "FLOAT":
                        if (Visit(context.listDatas().constant()[j]) is float f)
                            isSameType = true;
                        break;
                    case "STRING":
                        if (Visit(context.listDatas().constant()[j]) is string s)
                            isSameType = true;
                        break;
                    case "BOOL":
                        if (Visit(context.listDatas().constant()[j]) is bool b)
                            isSameType = true;
                        break;
                    default:
                        isSameType = false;
                        break;
                }

                if (!isSameType)
                    throw new Exception($"Valeur {j} ({context.listDatas().constant()[j].GetText()}) is not a {type} !");

                listFinal.Add(context.listDatas().constant()[j]);
            }
        }

        return listFinal;
    }


    public override object? VisitFunctionCall(FrenchyParser.FunctionCallContext context)
    {
        var name = context.IDENTIFIER().GetText();

        if (!Constants.ContainsKey(name))
            throw new Exception($"La fonction {name} n'est pas définie");

        if (Constants[name] is Func<object?> funcBasic)
            return funcBasic();
        else
        {
            var args = context.expression().Select(Visit).ToArray();
            var argSolo = context.expression(0).GetText();

            if (Constants[name] is Func<object?[], object?> funcArgs)
                return funcArgs(args);
            else if (Constants[name] is Func<string, int> funcSI)
                return funcSI(argSolo);
        }

        throw new Exception($"La variable {name} n'est pas une fonction");
    }
    public override object? VisitWhileBlock(FrenchyParser.WhileBlockContext context)
    {
        Func<object?, bool> condition = context.WHILE().GetText() == "pendant que" ? IsTrue : IsFalse;

        if (condition(Visit(context.expression())))
        {
            do
            {
                Visit(context.block());
            }
            while (condition(Visit(context.expression())));
        }

        return null;
    }
    public override object? VisitIfBlock(FrenchyParser.IfBlockContext context)
    {
        if (IsTrue(Visit(context.expression())))
            Visit(context.block());
        else
            Visit(context.elseIfBlock());

        return null;
    }

    public override object? VisitForBlock(FrenchyParser.ForBlockContext context)
    {
        var tempVarName = Visit(context.assignmentTemp(0)).ToString();

        if (!IsTrue(Visit(context.expression())))
        {
            do
            {
                Visit(context.block());
                Visit(context.assignmentTemp(1));
            }
            while (!IsTrue(Visit(context.expression())));
        }

        Temps.Remove(tempVarName);

        return null;
    }
    public override object? VisitForeachBlock(FrenchyParser.ForeachBlockContext context)
    {
        //TODO: Rework logic and clean code

        var datas = Visit(context.assignmentTempForeach());

        var lengthList = 0;
        var currentIndex = 0;
        IList tempList = null;
        var listIdentifier = context.IDENTIFIER().GetText();
        var dataType = context.assignmentTempForeach().dataTypes().GetText();

        //TODO: Need to verify that the data type of the datas is the same as the list one

        if (Variables.ContainsKey(listIdentifier))
        {
            tempList = (IList)Variables[listIdentifier];
            lengthList = tempList.Count;
        }
        else if (Constants.ContainsKey(listIdentifier))
        {
            tempList = (IList)Constants[listIdentifier];
            lengthList = tempList.Count;
        }

        do
        {
            switch (dataType)
            {
                case "INTEGER":
                    Temps.Add(datas.ToString(), (int)tempList[currentIndex]);
                    break;
                case "FLOAT":
                    Temps.Add(datas.ToString(), (float)tempList[currentIndex]);
                    break;
                case "STRING":
                    Temps.Add(datas.ToString(), (string)tempList[currentIndex]);
                    break;
                case "BOOL":
                    Temps.Add(datas.ToString(), (bool)tempList[currentIndex]);
                    break;
                default:
                    throw new Exception("Type non valide");
            }

            Visit(context.block());

            tempList[currentIndex] = Temps[datas.ToString()];
            Temps.Remove(datas.ToString());
        }
        while (currentIndex < lengthList);

        if (Variables.ContainsKey(listIdentifier))
        {
            Variables[listIdentifier] = tempList;
        }
        else if (Constants.ContainsKey(listIdentifier))
        {
            Constants[listIdentifier] = tempList;
        }

        return null;
    }

    public override object? VisitAdditiveExpression(FrenchyParser.AdditiveExpressionContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        var op = context.addOp().GetText();

        return op switch
        {
            "+" => Add(left, right),
            "-" => Substract(left, right),
            _ => throw new NotImplementedException()
        };
    }
    public override object? VisitMultiplicativeExpression(FrenchyParser.MultiplicativeExpressionContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        var op = context.multOp().GetText();

        return op switch
        {
            "*" => Multiply(left, right),
            "/" => Divide(left, right),
            "%" => Modulo(left, right),
            _ => throw new NotImplementedException()
        };
    }
    public override object? VisitComparisonExpression(FrenchyParser.ComparisonExpressionContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        var op = context.compareOp().GetText();

        return op switch
        {
            "==" => IsEquals(left, right),
            "!=" => NotEquals(left, right),
            ">" => GreaterThan(left, right),
            "<" => LessThan(left, right),
            ">=" => GreaterThanOrEqual(left, right),
            "<=" => LessThanOrEqual(left, right),
            _ => throw new NotImplementedException()
        };
    }
    #endregion

    #region BASIC MATHS
    private object? Add(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l + r;

        if (left is float lf && right is float rf)
            return lf + rf;

        if (left is int lInt && right is float rFloat)
            return lInt + rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat + rInt;

        if (left is string || right is string)
            return $"{left}{right}";

        throw new Exception($"Impossible de calculer des valeurs de types {left?.GetType()} et {right?.GetType()}!");
    }

    private object? Substract(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l - r;

        if (left is float lf && right is float rf)
            return lf - rf;

        if (left is int lInt && right is float rFloat)
            return lInt - rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat - rInt;

        throw new Exception($"Impossible de calculer des valeurs de types {left?.GetType()} et {right?.GetType()}!");
    }

    private object? Multiply(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l * r;

        if (left is float lf && right is float rf)
            return lf * rf;

        if (left is int lInt && right is float rFloat)
            return lInt * rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat * rInt;

        throw new Exception($"Impossible de calculer des valeurs de types {left?.GetType()} et {right?.GetType()}!");
    }

    private object? Divide(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l / r;

        if (left is float lf && right is float rf)
            return lf / rf;

        if (left is int lInt && right is float rFloat)
            return lInt / rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat / rInt;

        throw new Exception($"Impossible de calculer des valeurs de types {left?.GetType()} et {right?.GetType()}!");
    }

    private object? Modulo(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l % r;

        if (left is float lf && right is float rf)
            return lf % rf;

        if (left is int lInt && right is float rFloat)
            return lInt % rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat % rInt;

        throw new Exception($"Impossible de calculer des valeurs de types {left?.GetType()} et {right?.GetType()}!");
    }
    #endregion

    #region COMPARATORS
    private bool LessThan(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l < r;

        if (left is float lf && right is float rf)
            return lf < rf;

        if (left is int lInt && right is float rFloat)
            return lInt < rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat < rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    private bool GreaterThan(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l > r;

        if (left is float lf && right is float rf)
            return lf > rf;

        if (left is int lInt && right is float rFloat)
            return lInt > rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat > rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    private bool IsEquals(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l == r;

        if (left is float lf && right is float rf)
            return lf == rf;

        if (left is int lInt && right is float rFloat)
            return lInt == rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat == rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    private bool NotEquals(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l != r;

        if (left is float lf && right is float rf)
            return lf != rf;

        if (left is int lInt && right is float rFloat)
            return lInt != rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat != rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    private bool GreaterThanOrEqual(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l >= r;

        if (left is float lf && right is float rf)
            return lf >= rf;

        if (left is int lInt && right is float rFloat)
            return lInt >= rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat >= rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    private bool LessThanOrEqual(object? left, object? right)
    {
        if (left is int l && right is int r)
            return l <= r;

        if (left is float lf && right is float rf)
            return lf <= rf;

        if (left is int lInt && right is float rFloat)
            return lInt <= rFloat;

        if (left is float lFloat && right is int rInt)
            return lFloat <= rInt;

        throw new Exception($"Impossible de comparer des objets de type {left?.GetType()} et {right?.GetType()}!");
    }
    #endregion

    #region FUNCTION CALLS
    #endregion

    #region HELPERS
    private bool IsTrue(object? value)
    {
        if (value is bool b)
            return b;

        throw new Exception("Valeur retournée n'est pas un boolean");
    }

    public bool IsFalse(object? value) => !IsTrue(value);

    #endregion

    #region BASIC FUNCTIONS
    private object? MsgConsole(object?[] args)
    {
        foreach (var arg in args)
        {
            Console.WriteLine(arg);
        }

        return null;
    }

    private object? Pause()
    {
        Console.ReadKey();

        return null;
    }

    private int Size(string arg)
    {
        var length = 0;

        if (Variables.ContainsKey(arg))
        {
            IList tempList = (IList)Variables[arg];

            length = tempList.Count;
        }
        else if (Constants.ContainsKey(arg))
        {
            IList tempList = (IList)Constants[arg];

            length = tempList.Count;
        }

        return length;
    }
    #endregion
}