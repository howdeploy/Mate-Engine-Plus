// Inserts calls into MateEngine.ShojiBridge into the original
// rebuilt Assembly-CSharp.dll of the isolated MateEngine X3.4 Linux port.
// Only call sites are added;
// every hook falls through to the unchanged original code when the bridge
// does not handle it.
//
//   mono PatchAssembly.exe <original Assembly-CSharp.dll> <MateEngine.ShojiBridge.dll> <output Assembly-CSharp.dll>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class PatchAssembly
{
    const string BridgeAssembly = "MateEngine.ShojiBridge";
    const string BridgeType = "MateEngine.Shoji.ShojiBridge";

    static ModuleDefinition module;
    static TypeDefinition bridge;
    static readonly List<string> applied = new List<string>();
    static int bridgeCalls;

    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: PatchAssembly <original Assembly-CSharp.dll> <MateEngine.ShojiBridge.dll> <output Assembly-CSharp.dll> [assembly search dir...]");
            return 2;
        }
        string input = Path.GetFullPath(args[0]);
        string bridgePath = Path.GetFullPath(args[1]);
        string output = Path.GetFullPath(args[2]);
        if (input == output)
        {
            Console.Error.WriteLine("refusing to overwrite the input assembly");
            return 2;
        }

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(input));
        resolver.AddSearchDirectory(Path.GetDirectoryName(bridgePath));
        // The game's own Managed directory (UnityEngine.*, its mscorlib).
        for (int i = 3; i < args.Length; i++) resolver.AddSearchDirectory(Path.GetFullPath(args[i]));
        var parameters = new ReaderParameters { AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate };

        try
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input, parameters))
            using (AssemblyDefinition bridgeAssembly = AssemblyDefinition.ReadAssembly(bridgePath, parameters))
            {
                module = assembly.MainModule;
                if (module.AssemblyReferences.Any(r => r.Name == BridgeAssembly))
                    throw new InvalidOperationException("input is already patched; start from the original DLL");
                bridge = bridgeAssembly.MainModule.GetType(BridgeType);
                if (bridge == null) throw new InvalidOperationException(BridgeType + " not found in " + bridgePath);

                PatchWindowManager(Type("WindowManager"));
                PatchAnimatorController(Type("AvatarAnimatorController"));
                PatchSettingsMenuPosition(Type("SettingsMenuPosition"));
                PatchReactions(Type("PetVoiceReactionHandler"));
                PatchHands(Type("HandHolder"));
                Prologue(Method(Type("AvatarMouseTracking"), "LateUpdate", 0), first => new[] {
                    Instruction.Create(OpCodes.Call, Bridge("AllowPointerTracking")),
                    Instruction.Create(OpCodes.Brtrue, first),
                    Instruction.Create(OpCodes.Ret),
                });
                PatchPointerReads(Type("AvatarMouseTracking"), "DoHead", "DoSpine", "DoEye");
                PatchPointerReads(Type("AvatarBigScreenTouchHandler"), "HandleSpringBoneTouch");
                var touch = Method(Type("AvatarBigScreenTouchHandler"), "Update", 0);
                var touchButton = Single(touch.Body.Instructions, i => {
                    var called = i.Operand as MethodReference;
                    return called != null && called.DeclaringType.FullName == "UnityEngine.Input" && called.Name == "GetMouseButton";
                }, "Big Screen touch button");
                touchButton.Operand = Bridge("GetMouseButton");
                applied.Add("AvatarBigScreenTouchHandler::Update workspace visibility");
                PatchSeating(Type("AvatarWindowHandler"));
                ReturnFromBridge(Method(Type("MonitorHelper"), "GetTaskbarRectForWindow", 0), Bridge("TryGetTaskbarRect"));

                int calls = module.Types.SelectMany(t => t.Methods).Where(m => m.HasBody)
                    .Sum(m => m.Body.Instructions.Count(i => {
                        var method = i.Operand as MethodReference;
                        return method != null && method.DeclaringType.FullName == BridgeType;
                    }));
                if (calls != bridgeCalls) throw new InvalidOperationException("bridge call count mismatch");

                assembly.Write(output);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("patch failed: " + e.Message);
            return 1;
        }

        foreach (string line in applied) Console.WriteLine("patched " + line);
        Console.WriteLine("bridge call sites: " + bridgeCalls);
        return 0;
    }

    static void PatchWindowManager(TypeDefinition type)
    {
        FieldDefinition isDragging = Field(type, "_isDragging");
        FieldDefinition unityWindow = Field(type, "_unityWindow");
        FieldDefinition monitors = Field(type, "_monitors");
        foreach (string name in new[] { "OnPointerDown", "OnPointerUp" })
        {
            MethodDefinition method = Method(type, name, 1);
            Instruction store = Single(method.Body.Instructions, i => i.OpCode == OpCodes.Stfld &&
                ((FieldReference)i.Operand).Name == "_isDragging", name + " drag flag store");
            ExpandShortBranches(method.Body);
            ILProcessor processor = method.Body.GetILProcessor();
            foreach (Instruction instruction in new[] {
                Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Volatile),
                Instruction.Create(OpCodes.Ldfld, isDragging),
                Instruction.Create(OpCodes.Call, Bridge("SetClientDragging")),
            }) { processor.InsertAfter(store, instruction); store = instruction; }
            applied.Add("WindowManager::" + name + " drag lifecycle");
        }

        // Update: bridge housekeeping first; a lost release ends the drag.
        MethodDefinition update = Method(type, "Update", 0);
        MethodDefinition onPointerUp = Method(type, "OnPointerUp", 1);
        Prologue(update, first => new[]
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Volatile),
            Instruction.Create(OpCodes.Ldfld, isDragging),
            Instruction.Create(OpCodes.Call, Bridge("Tick")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldnull),
            Instruction.Create(OpCodes.Call, onPointerUp),
        });
        Instruction dragMove = Single(update.Body.Instructions, i => {
            var called = i.Operand as MethodReference;
            return called != null && called.DeclaringType.FullName == "WindowManager" &&
                called.Name == "SetWindowPosition" && called.Parameters.Count == 1 &&
                called.Parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
        }, "WindowManager::Update cursor drag move");
        dragMove.OpCode = OpCodes.Call;
        dragMove.Operand = Bridge("MoveDraggedWindow");

        // Vector2Int GetWindowPosition()
        MethodDefinition getWindowPosition = Method(type, "GetWindowPosition", 0);
        ReturnFromBridge(getWindowPosition, Bridge("TryGetWindowPosition"));

        // Vector2Int GetMousePosition()
        MethodDefinition getMouse = Method(type, "GetMousePosition", 0);
        ReturnFromBridge(getMouse, Bridge("TryGetPointer"));

        // bool GetMousePosition(out Vector2Int position)
        MethodDefinition getMouseOut = Method(type, "GetMousePosition", 1);
        Prologue(getMouseOut, first => new[]
        {
            Instruction.Create(OpCodes.Ldarg, getMouseOut.Parameters[0]),
            Instruction.Create(OpCodes.Call, Bridge("TryGetPointer")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Ret),
        });

        // void SetWindowPosition(Vector2Int position)
        MethodDefinition setPosition = type.Methods.Single(m => m.Name == "SetWindowPosition"
            && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int");
        Prologue(setPosition, first => new[]
        {
            Instruction.Create(OpCodes.Ldarg, setPosition.Parameters[0]),
            Instruction.Create(OpCodes.Call, Bridge("TrySetWindowPosition")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });

        // bool GetWindowRect(IntPtr window, out RectInt rect)
        MethodDefinition getRect = Method(type, "GetWindowRect", 2);
        var found = new VariableDefinition(module.TypeSystem.Boolean);
        getRect.Body.Variables.Add(found);
        getRect.Body.InitLocals = true;
        Prologue(getRect, first => new[]
        {
            Instruction.Create(OpCodes.Ldarg, getRect.Parameters[0]),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, unityWindow),
            Instruction.Create(OpCodes.Ldarg, getRect.Parameters[1]),
            Instruction.Create(OpCodes.Ldloca, found),
            Instruction.Create(OpCodes.Call, Bridge("TryGetWindowRect")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldloc, found),
            Instruction.Create(OpCodes.Ret),
        });

        MethodDefinition stacking = Method(type, "GetClientStackingList", 0);
        var handles = new VariableDefinition(stacking.ReturnType);
        stacking.Body.Variables.Add(handles);
        stacking.Body.InitLocals = true;
        Prologue(stacking, first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, unityWindow),
            Instruction.Create(OpCodes.Ldloca, handles),
            Instruction.Create(OpCodes.Call, Bridge("TryGetStackingList")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldloc, handles),
            Instruction.Create(OpCodes.Ret),
        });
        PatchHandleResult(Method(type, "GetWindowPid", 1), "TryGetWindowPid", null);
        PatchHandleResult(Method(type, "GetClassName", 1), "TryGetClassName", null);
        string[] flags = { "IsWindowVisible", "IsWindowMaximized", "IsWindowFullscreen", "IsDock", "IsDesktop" };
        for (int flag = 0; flag < flags.Length; flag++)
            PatchHandleResult(Method(type, flags[flag], flag == 0 ? 2 : 1), "TryWindowFlag", flag);

        // void QueryMonitors()
        MethodDefinition queryMonitors = Method(type, "QueryMonitors", 0);
        Prologue(queryMonitors, first => new[]
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldflda, monitors),
            Instruction.Create(OpCodes.Call, Bridge("TryQueryMonitors")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });

        // UpdateInputMask: filter the pixel copy right after
        // "imageBytes = GetImageData(xImagePtr, width, height).Data;"
        MethodDefinition updateInputMask = Method(type, "UpdateInputMask", 2);
        MethodBody body = updateInputMask.Body;
        Instruction getImageData = Single(body.Instructions, i => IsCall(i, "GetImageData"), "GetImageData call");
        Instruction loadData = getImageData.Next;
        Instruction pixelsStore = loadData != null ? loadData.Next : null;
        if (loadData == null || loadData.OpCode != OpCodes.Ldfld || ((FieldReference)loadData.Operand).Name != "Data")
            throw new InvalidOperationException("UpdateInputMask: unexpected IL after GetImageData");
        VariableDefinition pixels = StoredVariable(body, pixelsStore);
        ExpandShortBranches(body);
        ILProcessor il = body.GetILProcessor();
        Instruction[] filter =
        {
            Instruction.Create(OpCodes.Ldloc, pixels),
            Instruction.Create(OpCodes.Ldarg, updateInputMask.Parameters[0]),
            Instruction.Create(OpCodes.Ldarg, updateInputMask.Parameters[1]),
            Instruction.Create(OpCodes.Call, Bridge("FilterInputPixels")),
        };
        Instruction after = pixelsStore;
        foreach (Instruction instruction in filter)
        {
            il.InsertAfter(after, instruction);
            after = instruction;
        }
        applied.Add("WindowManager::UpdateInputMask");
    }

    static void PatchHandleResult(MethodDefinition method, string hook, int? flag)
    {
        var value = new VariableDefinition(method.ReturnType);
        method.Body.Variables.Add(value);
        method.Body.InitLocals = true;
        Prologue(method, first => {
            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldarg, method.Parameters[0]) };
            if (flag.HasValue) instructions.Add(Instruction.Create(OpCodes.Ldc_I4, flag.Value));
            instructions.Add(Instruction.Create(OpCodes.Ldloca, value));
            instructions.Add(Instruction.Create(OpCodes.Call, Bridge(hook)));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, first));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, value));
            instructions.Add(Instruction.Create(OpCodes.Ret));
            return instructions.ToArray();
        });
    }

    static void PatchReactions(TypeDefinition type)
    {
        ReturnFromBridge(Method(type, "IsOccludedByOS", 0), Bridge("TryIsOccluded"));
        MethodDefinition update = Method(type, "Update", 0);
        Instruction mouse = Single(update.Body.Instructions, i => {
            var target = i.Operand as MethodReference;
            return target != null && target.DeclaringType.FullName == "UnityEngine.Input" && target.Name == "get_mousePosition";
        }, "PetVoiceReactionHandler Input.mousePosition");
        mouse.Operand = Bridge("GetUnityMousePosition");
        applied.Add("PetVoiceReactionHandler::Update pointer");
    }

    static void PatchHands(TypeDefinition type)
    {
        foreach (string name in new[] { "ComputeWorldWeight", "GetProjectedMouseTarget" })
        {
            MethodDefinition method = Method(type, name, name == "ComputeWorldWeight" ? 1 : 0);
            Instruction projection = Single(method.Body.Instructions, i => {
                var target = i.Operand as MethodReference;
                return target != null && target.DeclaringType.FullName == "UnityEngine.Camera" &&
                    target.Name == "ScreenToWorldPoint" && target.Parameters.Count == 1;
            }, "HandHolder::" + name + " projection");
            // Camera and Vector3 are already on the stack; the static bridge
            // consumes both and returns the same Vector3 as the instance call.
            projection.OpCode = OpCodes.Call;
            projection.Operand = Bridge("ScreenToWorldPointer");
            applied.Add("HandHolder::" + name + " pointer projection");
        }
    }

    static void PatchPointerReads(TypeDefinition type, params string[] names)
    {
        foreach (string name in names)
        {
            var method = Method(type, name, 0);
            var read = Single(method.Body.Instructions, i => {
                var target = i.Operand as MethodReference;
                return target != null && target.DeclaringType.FullName == "UnityEngine.Input" &&
                    target.Name == "get_mousePosition";
            }, type.Name + "::" + name + " Input.mousePosition");
            read.OpCode = OpCodes.Call;
            read.Operand = Bridge("GetUnityMousePosition");
            applied.Add(type.Name + "::" + name + " compositor pointer");
        }
    }

    static void PatchAnimatorController(TypeDefinition type)
    {
        MethodDefinition update = Method(type, "Update", 0);
        Instruction call = Single(update.Body.Instructions, i =>
        {
            var target = i.OpCode == OpCodes.Call ? i.Operand as MethodReference : null;
            return target != null && target.Name == "GetMouseButtonUp" && target.DeclaringType.FullName == "UnityEngine.Input";
        }, "Input.GetMouseButtonUp call");
        call.Operand = Bridge("GetMouseButtonUp");
        applied.Add("AvatarAnimatorController::Update");
    }

    static void PatchSeating(TypeDefinition type)
    {
        MethodDefinition snap = Method(type, "TrySnap", 0);
        var snapProbes = snap.Body.Instructions.Where(i => {
            var called = i.Operand as MethodReference;
            return called != null && called.DeclaringType.FullName == "AvatarWindowHandler" &&
                called.Name == "ComputeZoneDesktop";
        }).ToArray();
        if (snapProbes.Length != 2)
            throw new InvalidOperationException("TrySnap: acquisition probe call count changed");
        snapProbes[1].OpCode = OpCodes.Call;
        snapProbes[1].Operand = Bridge("ComputeSnapProbe");
        applied.Add("AvatarWindowHandler::TrySnap dock acquisition probe");
        MethodDefinition keepSeat = Method(type, "IsStillNearSnappedWindow", 0);
        var keep = new VariableDefinition(module.TypeSystem.Boolean);
        keepSeat.Body.Variables.Add(keep);
        keepSeat.Body.InitLocals = true;
        Prologue(keepSeat, first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldloca, keep),
            Instruction.Create(OpCodes.Call, Bridge("TryKeepSeat")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldloc, keep),
            Instruction.Create(OpCodes.Ret),
        });
        Prologue(Method(type, "PinToTarget", 1), first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("TryPinSeat")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });
        TypeDefinition hide = Type("AvatarHideHandler");
        Prologue(Method(hide, "Update", 0), first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("BeforeEdgeUpdate")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });
        ReturnFromBridge(Method(hide, "GetCurrentMonitorRect", 1), Bridge("TryGetEdgeMonitor"));
        // 3.4's private wrapper returns its own RECT, not Unity's RectInt.
        // It calls GetCurrentMonitorRect, so one typed hook covers both paths.
        Prologue(Method(hide, "Unsnap", 0), first => new[] {
            Instruction.Create(OpCodes.Call, Bridge("EdgeReleased")),
        });
        Prologue(Method(type, "ClearSnapAndHide", 1), first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("SeatReleased")),
        });
        Prologue(Method(type, "Update", 0), first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("BeforeSeatingUpdate")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });
        Prologue(Method(type, "UpdateOccluderQuadsFrameSync", 0), first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("PrepareSeatingOcclusion")),
        });
        MethodDefinition projection = Method(type, "ComputeDesktopFromWorld", 3);
        Instruction[] clamps = projection.Body.Instructions.Where(i => {
            var called = i.Operand as MethodReference;
            return called != null && called.DeclaringType.FullName == "UnityEngine.Mathf" &&
                called.Name == "Clamp" && called.Parameters.Count == 3 &&
                called.Parameters[0].ParameterType.FullName == "System.Single";
        }).ToArray();
        if (clamps.Length != 2)
            throw new InvalidOperationException("ComputeDesktopFromWorld: screen clamp count changed");
        ExpandShortBranches(projection.Body);
        foreach (Instruction clamp in clamps)
        {
            clamp.OpCode = OpCodes.Call;
            clamp.Operand = Bridge("SeatScreenCoordinate");
        }
        MethodDefinition calibrate = Method(type, "CalibrateSeatAnchorToDesktopY", 1);
        Prologue(calibrate, first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, Bridge("TryCalibrateSeat")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Ret),
        });

        MethodDefinition transient = Method(Type("WindowManager"), "SetTransientFor", 2);
        Prologue(transient, first => new[] {
            Instruction.Create(OpCodes.Ldarg, transient.Parameters[0]),
            Instruction.Create(OpCodes.Call, Bridge("TrySetSeatingTransient")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });
        applied.Add("AvatarWindowHandler: workspace pause, depth occlusion and seat projection/calibration");
        MethodDefinition seatWorld = Method(type, "GetSeatWorldCurrent", 0);
        var contact = new VariableDefinition(seatWorld.ReturnType);
        seatWorld.Body.Variables.Add(contact);
        seatWorld.Body.InitLocals = true;
        Prologue(seatWorld, first => new[] {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldloca, contact),
            Instruction.Create(OpCodes.Call, Bridge("TrySeatWorldCurrent")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldloc, contact),
            Instruction.Create(OpCodes.Ret),
        });
        applied.Add("WindowManager::SetTransientFor virtual handle guard");
    }

    static void PatchSettingsMenuPosition(TypeDefinition type)
    {
        MethodDefinition update = Method(type, "Update", 0);
        Prologue(update, first => new[]
        {
            Instruction.Create(OpCodes.Call, Bridge("MenuLayoutActive")),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ret),
        });

        if (type.Methods.Any(m => m.Name == "LateUpdate"))
            throw new InvalidOperationException("SettingsMenuPosition already has LateUpdate");
        var lateUpdate = new MethodDefinition("LateUpdate", MethodAttributes.Private | MethodAttributes.HideBySig, module.TypeSystem.Void);
        ILProcessor il = lateUpdate.Body.GetILProcessor();
        il.Append(Instruction.Create(OpCodes.Ldarg_0));
        il.Append(Instruction.Create(OpCodes.Call, Bridge("LayoutMenus")));
        il.Append(Instruction.Create(OpCodes.Ret));
        type.Methods.Add(lateUpdate);
        applied.Add("SettingsMenuPosition::LateUpdate (new)");
    }

    // "if (Bridge.TryX(out value)) return value;" before the original body.
    static void ReturnFromBridge(MethodDefinition method, MethodReference tryGet)
    {
        var value = new VariableDefinition(method.ReturnType);
        method.Body.Variables.Add(value);
        method.Body.InitLocals = true;
        Prologue(method, first => new[]
        {
            Instruction.Create(OpCodes.Ldloca, value),
            Instruction.Create(OpCodes.Call, tryGet),
            Instruction.Create(OpCodes.Brfalse, first),
            Instruction.Create(OpCodes.Ldloc, value),
            Instruction.Create(OpCodes.Ret),
        });
    }

    static void Prologue(MethodDefinition method, Func<Instruction, Instruction[]> build)
    {
        MethodBody body = method.Body;
        ExpandShortBranches(body);
        Instruction first = body.Instructions[0];
        ILProcessor il = body.GetILProcessor();
        foreach (Instruction instruction in build(first)) il.InsertBefore(first, instruction);
        applied.Add(method.DeclaringType.Name + "::" + method.Name + "(" + method.Parameters.Count + ")");
    }

    // Inserted code shifts branch distances; long forms are always valid.
    static void ExpandShortBranches(MethodBody body)
    {
        var map = new Dictionary<Code, OpCode>
        {
            { Code.Br_S, OpCodes.Br }, { Code.Brfalse_S, OpCodes.Brfalse }, { Code.Brtrue_S, OpCodes.Brtrue },
            { Code.Beq_S, OpCodes.Beq }, { Code.Bge_S, OpCodes.Bge }, { Code.Bgt_S, OpCodes.Bgt },
            { Code.Ble_S, OpCodes.Ble }, { Code.Blt_S, OpCodes.Blt }, { Code.Bne_Un_S, OpCodes.Bne_Un },
            { Code.Bge_Un_S, OpCodes.Bge_Un }, { Code.Bgt_Un_S, OpCodes.Bgt_Un }, { Code.Ble_Un_S, OpCodes.Ble_Un },
            { Code.Blt_Un_S, OpCodes.Blt_Un }, { Code.Leave_S, OpCodes.Leave },
        };
        foreach (Instruction instruction in body.Instructions)
        {
            OpCode longForm;
            if (map.TryGetValue(instruction.OpCode.Code, out longForm)) instruction.OpCode = longForm;
        }
    }

    static VariableDefinition StoredVariable(MethodBody body, Instruction store)
    {
        if (store == null) throw new InvalidOperationException("missing store instruction");
        switch (store.OpCode.Code)
        {
            case Code.Stloc_0: return body.Variables[0];
            case Code.Stloc_1: return body.Variables[1];
            case Code.Stloc_2: return body.Variables[2];
            case Code.Stloc_3: return body.Variables[3];
            case Code.Stloc_S:
            case Code.Stloc: return (VariableDefinition)store.Operand;
        }
        throw new InvalidOperationException("expected stloc, found " + store.OpCode);
    }

    static bool IsCall(Instruction instruction, string name)
    {
        var target = instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt
            ? instruction.Operand as MethodReference : null;
        return target != null && target.Name == name;
    }

    static MethodReference Bridge(string name)
    {
        bridgeCalls++;
        return module.ImportReference(Single(bridge.Methods, m => m.Name == name && m.IsPublic && m.IsStatic, BridgeType + "." + name));
    }

    static TypeDefinition Type(string name)
    {
        TypeDefinition type = module.GetType(name);
        if (type == null) throw new InvalidOperationException("type " + name + " not found");
        return type;
    }

    static MethodDefinition Method(TypeDefinition type, string name, int parameterCount)
    {
        return Single(type.Methods, m => m.Name == name && m.Parameters.Count == parameterCount && m.HasBody,
            type.Name + "::" + name + "/" + parameterCount);
    }

    static FieldDefinition Field(TypeDefinition type, string name)
    {
        return Single(type.Fields, f => f.Name == name, type.Name + "::" + name);
    }

    static T Single<T>(IEnumerable<T> items, Func<T, bool> predicate, string what)
    {
        List<T> matches = items.Where(predicate).ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException("expected exactly one " + what + ", found " + matches.Count);
        return matches[0];
    }
}
