namespace Prospect.Unreal.Core.Names
{
    /// <summary>
    ///     Hardcoded names in Unreal Engine 4.26.2. See "UnrealNames.inl"
    /// </summary>
    public static class UnrealNames
    {
        public const int MaxNetworkedHardcodedName = 410;
        
        static UnrealNames()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var names = new Dictionary<int, string>();
            
            // Special zero value, meaning no name.
            names.Add(0, "None");

            // Class property types (name indices are significant for serialization).
            names.Add(1, "ByteProperty");
            names.Add(2, "IntProperty");
            names.Add(3, "BoolProperty");
            names.Add(4, "FloatProperty");
            names.Add(5, "ObjectProperty"); // ClassProperty shares the same tag
            names.Add(6, "NameProperty");
            names.Add(7, "DelegateProperty");
            names.Add(8, "DoubleProperty");
            names.Add(9, "ArrayProperty");
            names.Add(10, "StructProperty");
            names.Add(11, "VectorProperty");
            names.Add(12, "RotatorProperty");
            names.Add(13, "StrProperty");
            names.Add(14, "TextProperty");
            names.Add(15, "InterfaceProperty");
            names.Add(16, "MulticastDelegateProperty");
            //Names.Add(17, "Available");
            names.Add(18, "LazyObjectProperty");
            names.Add(19, "SoftObjectProperty"); // SoftClassProperty shares the same tag
            names.Add(20, "UInt64Property");
            names.Add(21, "UInt32Property");
            names.Add(22, "UInt16Property");
            names.Add(23, "Int64Property");
            names.Add(25, "Int16Property");
            names.Add(26, "Int8Property");
            //Names.Add(27, "Available");
            names.Add(28, "MapProperty");
            names.Add(29, "SetProperty");

            // Special packages.
            names.Add(30, "Core");
            names.Add(31, "Engine");
            names.Add(32, "Editor");
            names.Add(33, "CoreUObject");

            // More class properties
            names.Add(34, "EnumProperty");

            // Special types.
            names.Add(50, "Cylinder");
            names.Add(51, "BoxSphereBounds");
            names.Add(52, "Sphere");
            names.Add(53, "Box");
            names.Add(54, "Vector2D");
            names.Add(55, "IntRect");
            names.Add(56, "IntPoint");
            names.Add(57, "Vector4");
            names.Add(58, "Name");
            names.Add(59, "Vector");
            names.Add(60, "Rotator");
            names.Add(61, "SHVector");
            names.Add(62, "Color");
            names.Add(63, "Plane");
            names.Add(64, "Matrix");
            names.Add(65, "LinearColor");
            names.Add(66, "AdvanceFrame");
            names.Add(67, "Pointer");
            names.Add(68, "Double");
            names.Add(69, "Quat");
            names.Add(70, "Self");
            names.Add(71, "Transform");

            // Object class names.
            names.Add(100, "Object");
            names.Add(101, "Camera");
            names.Add(102, "Actor");
            names.Add(103, "ObjectRedirector");
            names.Add(104, "ObjectArchetype");
            names.Add(105, "Class");
            names.Add(106, "ScriptStruct");
            names.Add(107, "Function");
            names.Add(108, "Pawn");

            // Misc.
            names.Add(200, "State");
            names.Add(201, "TRUE");
            names.Add(202, "FALSE");
            names.Add(203, "Enum");
            names.Add(204, "Default");
            names.Add(205, "Skip");
            names.Add(206, "Input");
            names.Add(207, "Package");
            names.Add(208, "Groups");
            names.Add(209, "Interface");
            names.Add(210, "Components");
            names.Add(211, "Global");
            names.Add(212, "Super");
            names.Add(213, "Outer");
            names.Add(214, "Map");
            names.Add(215, "Role");
            names.Add(216, "RemoteRole");
            names.Add(217, "PersistentLevel");
            names.Add(218, "TheWorld");
            names.Add(219, "PackageMetaData");
            names.Add(220, "InitialState");
            names.Add(221, "Game");
            names.Add(222, "SelectionColor");
            names.Add(223, "UI");
            names.Add(224, "ExecuteUbergraph");
            names.Add(225, "DeviceID");
            names.Add(226, "RootStat");
            names.Add(227, "MoveActor");
            names.Add(230, "All");
            names.Add(231, "MeshEmitterVertexColor");
            names.Add(232, "TextureOffsetParameter");
            names.Add(233, "TextureScaleParameter");
            names.Add(234, "ImpactVel");
            names.Add(235, "SlideVel");
            names.Add(236, "TextureOffset1Parameter");
            names.Add(237, "MeshEmitterDynamicParameter");
            names.Add(238, "ExpressionInput");
            names.Add(239, "Untitled");
            names.Add(240, "Timer");
            names.Add(241, "Team");
            names.Add(242, "Low");
            names.Add(243, "High");
            names.Add(244, "NetworkGUID");
            names.Add(245, "GameThread");
            names.Add(246, "RenderThread");
            names.Add(247, "OtherChildren");
            names.Add(248, "Location");
            names.Add(249, "Rotation");
            names.Add(250, "BSP");
            names.Add(251, "EditorSettings");
            names.Add(252, "AudioThread");
            names.Add(253, "ID");
            names.Add(254, "UserDefinedEnum");
            names.Add(255, "Control");
            names.Add(256, "Voice");
            names.Add(257, " Zlib");
            names.Add(258, " Gzip");
            names.Add(259, " LZ4");
            names.Add(260, " Mobile");

            // Online
            names.Add(280, "DGram");
            names.Add(281, "Stream");
            names.Add(282, "GameNetDriver");
            names.Add(283, "PendingNetDriver");
            names.Add(284, "BeaconNetDriver");
            names.Add(285, "FlushNetDormancy");
            names.Add(286, "DemoNetDriver");
            names.Add(287, "GameSession");
            names.Add(288, "PartySession");
            names.Add(289, "GamePort");
            names.Add(290, "BeaconPort");
            names.Add(291, "MeshPort");
            names.Add(292, "MeshNetDriver");
            names.Add(293, "LiveStreamVoice");
            names.Add(294, "LiveStreamAnimation");

            // Texture settings.
            names.Add(300, "Linear");
            names.Add(301, "Point");
            names.Add(302, "Aniso");
            names.Add(303, "LightMapResolution");

            // Sound.
            //Names.Add(310, "");
            names.Add(311, "UnGrouped");
            names.Add(312, "VoiceChat");

            // Optimized replication.
            names.Add(320, "Playing");
            names.Add(322, "Spectating");
            names.Add(325, "Inactive");

            // Log messages.
            names.Add(350, "PerfWarning");
            names.Add(351, "Info");
            names.Add(352, "Init");
            names.Add(353, "Exit");
            names.Add(354, "Cmd");
            names.Add(355, "Warning");
            names.Add(356, "Error");

            // File format backwards-compatibility.
            names.Add(400, "FontCharacter");
            names.Add(401, "InitChild2StartBone");
            names.Add(402, "SoundCueLocalized");
            names.Add(403, "SoundCue");
            names.Add(404, "RawDistributionFloat");
            names.Add(405, "RawDistributionVector");
            names.Add(406, "InterpCurveFloat");
            names.Add(407, "InterpCurveVector2D");
            names.Add(408, "InterpCurveVector");
            names.Add(409, "InterpCurveTwoVectors");
            names.Add(410, "InterpCurveQuat");

            names.Add(450, "AI");
            names.Add(451, "NavMesh");

            names.Add(500, "PerformanceCapture");

            // Special config names - not required to be consistent for network replication
            names.Add(600, "EditorLayout");
            names.Add(601, "EditorKeyBindings");
            names.Add(602, "GameUserSettings");
            
            names.Add(700, "Filename");
            names.Add(701, "Lerp");
            names.Add(702, "Root");
            
            // Save.
            Names = names.ToDictionary(x => (EName) x.Key, y => y.Value);
            MaxHardcodedNameIndex = (int)Names.Last().Key + 1;
        }

        public static IReadOnlyDictionary<EName, string> Names { get; }
        public static int MaxHardcodedNameIndex { get; }
    }
}