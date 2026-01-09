
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;
using System.Linq;
using System.Collections.Generic;
using System;

public partial class ModuleWeaver
{
    public MethodDefinition CloneMethod(TypeDefinition type, MethodDefinition method, string prefix = "_original_")
    {
        // 1. Clone
        var cloned = CloneMethodSignature(method, (prefix + method.Name).Replace(".", string.Empty));
        CloneMethodBody(method, cloned);

        WriteInfo($"Cloning method: {method.FullName} to {cloned.FullName}");

        // 1.a Mark as [CompilerGenerated]
        AddCompilerGeneratedAttribute(cloned);

        // 2. Add cloned method
        type.Methods.Add(cloned);

        // 3. Redirect original
        RedirectMethodToClone(method, cloned);

        // 4. Return cloned method
        return cloned;
    }

    private MethodDefinition CloneMethodSignature(MethodDefinition source, string newName, bool makePrivate = true)
    {
        var clone = new MethodDefinition(newName, source.Attributes, source.ReturnType)
        {
            ImplAttributes = source.ImplAttributes,
            CallingConvention = source.CallingConvention,
            HasThis = source.HasThis,
            ExplicitThis = source.ExplicitThis
        };

        if (makePrivate)
        {
            clone.Attributes &= ~Mono.Cecil.MethodAttributes.Public;
            clone.Attributes |= Mono.Cecil.MethodAttributes.Private;
        }

        //if (makeInternal)
        //{
        //    clone.Attributes &= ~Mono.Cecil.MethodAttributes.Public;
        //    clone.Attributes &= ~Mono.Cecil.MethodAttributes.Private;
        //    clone.Attributes |= Mono.Cecil.MethodAttributes.Assembly;
        //}

        // Remove abstract/virtual flags
        clone.Attributes &= ~Mono.Cecil.MethodAttributes.Abstract;
        clone.Attributes &= ~Mono.Cecil.MethodAttributes.Virtual;

        // Generic parameters
        foreach (var gp in source.GenericParameters)
            clone.GenericParameters.Add(new GenericParameter(gp.Name, clone));

        // Parameters
        foreach (var p in source.Parameters)
            clone.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));

        return clone;
    }

    private void CloneMethodBody(MethodDefinition source, MethodDefinition target)
    {
        target.Body.SimplifyMacros();
        target.Body.InitLocals = source.Body.InitLocals;

        // Constructors must NOT look like constructors
        if (source.IsConstructor)
        {
            target.Attributes &= ~MethodAttributes.SpecialName;
            target.Attributes &= ~MethodAttributes.RTSpecialName;
        }

        var il = target.Body.GetILProcessor();

        // === Variables ===
        var variableMap = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var v in source.Body.Variables)
        {
            var nv = new VariableDefinition(v.VariableType);
            target.Body.Variables.Add(nv);
            variableMap[v] = nv;
        }

        // === Instructions ===
        var instructionMap = new Dictionary<Instruction, Instruction>();

        var instructions = source.Body.Instructions;
        int startIndex = 0;

        if (source.IsConstructor)
        {
            // Find first instruction AFTER base(...) / this(...)
            for (int i = 0; i < instructions.Count; i++)
            {
                if (IsBaseCtorCall(instructions[i]))
                {
                    startIndex = i + 1;
                    break;
                }
            }
        }

        for (int i = startIndex; i < instructions.Count; i++)
        {
            var src = instructions[i];
            var dst = CloneInstruction(src);
            il.Append(dst);
            instructionMap[src] = dst;
        }

        // === Fix operands ===
        foreach (var dst in instructionMap.Values)
        {
            if (dst.Operand is Instruction t)
                dst.Operand = instructionMap[t];
            else if (dst.Operand is Instruction[] ts)
                dst.Operand = ts.Select(x => instructionMap[x]).ToArray();
            else if (dst.Operand is VariableDefinition v)
                dst.Operand = variableMap[v];
        }

        // === Exception handlers ===
        foreach (var eh in source.Body.ExceptionHandlers)
        {
            // Drop handlers that touch ctor chain
            if (!instructionMap.ContainsKey(eh.TryStart) ||
                !instructionMap.ContainsKey(eh.HandlerStart))
                continue;

            target.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
            {
                CatchType = eh.CatchType,
                TryStart = instructionMap[eh.TryStart],
                TryEnd = instructionMap[eh.TryEnd],
                HandlerStart = instructionMap[eh.HandlerStart],
                HandlerEnd = instructionMap[eh.HandlerEnd]
            });
        }

        target.Body.OptimizeMacros();
    }

    private static Instruction CloneInstruction(Instruction i)
    {
        if (i.Operand == null)
            return Instruction.Create(i.OpCode);

        return i.Operand switch
        {
            sbyte v => Instruction.Create(i.OpCode, v),
            byte v => Instruction.Create(i.OpCode, v),
            int v => Instruction.Create(i.OpCode, v),
            long v => Instruction.Create(i.OpCode, v),
            float v => Instruction.Create(i.OpCode, v),
            double v => Instruction.Create(i.OpCode, v),
            string v => Instruction.Create(i.OpCode, v),

            MethodReference v => Instruction.Create(i.OpCode, v),
            FieldReference v => Instruction.Create(i.OpCode, v),
            TypeReference v => Instruction.Create(i.OpCode, v),

            ParameterDefinition v => Instruction.Create(i.OpCode, v),
            VariableDefinition v => Instruction.Create(i.OpCode, v),

            Instruction v => Instruction.Create(i.OpCode, v),
            Instruction[] v => Instruction.Create(i.OpCode, v),

            _ => throw new NotSupportedException(
                $"Unsupported operand type: {i.Operand.GetType().FullName}")
        };
    }

    private void RedirectMethodToClone(MethodDefinition original, MethodDefinition clone)
    {
        var il = original.Body.GetILProcessor();

        // Capture ONLY the ctor-chain block
        List<Instruction> ctorChain = null;

        var isConstructor = original.IsConstructor && !original.IsStatic;
        if (isConstructor)
            ctorChain = ExtractCtorChain(original);

        // Wipe method body completely
        original.Body.Instructions.Clear();
        original.Body.Variables.Clear();
        original.Body.ExceptionHandlers.Clear();
        original.Body.InitLocals = false;

        if (!isConstructor)
        {
            // ===== Normal method =====
            if (!original.IsStatic)
                il.Append(il.Create(OpCodes.Ldarg_0));

            foreach (var p in original.Parameters)
                il.Append(il.Create(OpCodes.Ldarg, p));

            il.Append(il.Create(OpCodes.Call, clone));
            il.Append(il.Create(OpCodes.Ret));
            return;
        }

        // ===== Constructor =====

        // 1. Replay ONLY the original ctor-chain instructions
        var map = new Dictionary<Instruction, Instruction>();

        foreach (var i in ctorChain)
        {
            var ni = CloneInstruction(i);

            // Import ctor reference
            if (ni.Operand is MethodReference mr)
                ni.Operand = original.Module.ImportReference(mr);

            il.Append(ni);
            map[i] = ni;
        }

        // Fix branch targets inside ctor block (rare but legal)
        foreach (var ni in map.Values)
        {
            if (ni.Operand is Instruction t)
                ni.Operand = map[t];
            else if (ni.Operand is Instruction[] ts)
                ni.Operand = ts.Select(x => map[x]).ToArray();
        }

        // 2. Call extracted constructor body
        il.Append(il.Create(OpCodes.Ldarg_0));
        foreach (var p in original.Parameters)
            il.Append(il.Create(OpCodes.Ldarg, p));
        il.Append(il.Create(OpCodes.Call, clone));

        il.Append(il.Create(OpCodes.Ret));
    }

    private static List<Instruction> ExtractCtorChain(MethodDefinition ctor)
    {
        var result = new List<Instruction>();

        foreach (var i in ctor.Body.Instructions)
        {
            result.Add(i);

            if (IsConstructorCall(i))
                break;
        }

        if (!result.Any(IsConstructorCall))
            throw new InvalidOperationException(
                $"Constructor chain not found in {ctor.FullName}");

        return result;
    }

    private static bool IsConstructorCall(Instruction i)
    {
        return i.Operand is MethodReference mr &&
               mr.Name == ".ctor" &&
               (i.OpCode == OpCodes.Call ||
                i.OpCode == OpCodes.Callvirt ||
                i.OpCode == OpCodes.Newobj);
    }

    private static bool IsBaseCtorCall(Instruction i)
    {
        return i.OpCode == OpCodes.Call &&
               i.Operand is MethodReference mr &&
               mr.Name == ".ctor";
    }
}
