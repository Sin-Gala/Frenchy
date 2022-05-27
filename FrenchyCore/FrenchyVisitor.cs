﻿using Antlr4.Runtime.Misc;
using Frenchy.FrenchyCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class FrenchyVisitor : FrenchyBaseVisitor<object?>
{
    #region CONSTRUCTOR
    public FrenchyVisitor()
    {
        // Set const functions and variables
        Constants["Pi"] = 3.14f;
        Constants["MsgConsole"] = new Func<object?[], object?>(MsgConsole);
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

        if ((tempValue is int || tempValue is float) && !Temps.ContainsKey(tempVarName))
            Temps[tempVarName] = tempValue;
        else if (tempValue is not int && tempValue is not float)
            throw new Exception($"Variable {tempVarName} n'est pas un int ou un float!");

        return tempVarName;
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

    public override object? VisitFunctionCall(FrenchyParser.FunctionCallContext context)
    {
        var name = context.IDENTIFIER().GetText();
        var args = context.expression().Select(Visit).ToArray();

        if (!Constants.ContainsKey(name))
            throw new Exception($"La fonction {name} n'est pas définie");

        if (Constants[name] is not Func<object?[], object?> func)
            throw new Exception($"La variable {name} n'est pas une fonction");

        return func(args);
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
        var tempVarName = Visit(context.assignmentTemp()).ToString();

        if (IsFalse(Visit(context.expression(0))))
        {
            do
            {
                //Visit(context.block());
                Visit(context.expression(1));
            }
            while (IsFalse(Visit(context.expression(0))));
        }

        Temps.Remove(tempVarName);

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

    #region BASIC LOOPS
    private bool IsTrue(object? value)
    {
        if (value is bool b)
            return b;

        throw new Exception("Valeur retournée n'est pas un boolean");
    }

    public bool IsFalse(object? value) => !IsTrue(value);
    #endregion

    #region FUNCTION CALLS
    #endregion

    #region COMMENTS
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

    #endregion
}