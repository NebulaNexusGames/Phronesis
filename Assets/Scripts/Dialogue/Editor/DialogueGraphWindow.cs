using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphWindow : EditorWindow
{
    private DialogueAsset asset;
    private DialogueGraphView view;

    [MenuItem("Assets/Edit Dialogue Graph", priority = 2000)]
    private static void OpenFromProjectBrowser()
    {
        Open(Selection.activeObject as DialogueAsset);
    }

    [MenuItem("Assets/Edit Dialogue Graph", true)]
    private static bool ValidateOpenFromProjectBrowser()
    {
        return Selection.activeObject is DialogueAsset;
    }

    public static void Open(DialogueAsset dialogueAsset)
    {
        if (dialogueAsset == null) return;
        DialogueGraphWindow window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Диалог: " + dialogueAsset.name);
        window.Load(dialogueAsset);
    }

    private void Load(DialogueAsset dialogueAsset)
    {
        asset = dialogueAsset;
        BuildInterface();
        if (view != null) view.Populate(asset);
    }

    private void BuildInterface()
    {
        rootVisualElement.Clear();

        view = new DialogueGraphView();
        view.StretchToParentSize();
        rootVisualElement.Add(view);

        Toolbar toolbar = new Toolbar();

        Label header = new Label(asset != null ? asset.name : "(нет ассета)");
        header.style.marginLeft = 8f;
        header.style.marginRight = 12f;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        toolbar.Add(header);

        toolbar.Add(new ToolbarButton(view.AddNewLineNode) { text = "+ Реплика" });
        toolbar.Add(new ToolbarButton(Save) { text = "Сохранить" });
        toolbar.Add(new ToolbarButton(() => view.FrameAll()) { text = "Вписать в экран" });

        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(BuildCharacterSettingsRow());
    }

    private VisualElement BuildCharacterSettingsRow()
    {
        Toolbar row = new Toolbar();

        TextField npcNameField = new TextField("Персонаж") { value = asset.npcName };
        npcNameField.style.width = 220f;
        npcNameField.RegisterValueChangedCallback(evt =>
        {
            asset.npcName = evt.newValue;
            EditorUtility.SetDirty(asset);
        });
        row.Add(npcNameField);

        ObjectField npcPortraitField = new ObjectField("портрет") { objectType = typeof(Sprite), value = asset.npcPortrait };
        npcPortraitField.style.width = 250f;
        npcPortraitField.RegisterValueChangedCallback(evt =>
        {
            asset.npcPortrait = evt.newValue as Sprite;
            EditorUtility.SetDirty(asset);
        });
        row.Add(npcPortraitField);

        TextField playerNameField = new TextField("Игрок") { value = asset.playerName };
        playerNameField.style.width = 220f;
        playerNameField.RegisterValueChangedCallback(evt =>
        {
            asset.playerName = evt.newValue;
            EditorUtility.SetDirty(asset);
        });
        row.Add(playerNameField);

        ObjectField playerPortraitField = new ObjectField("портрет") { objectType = typeof(Sprite), value = asset.playerPortrait };
        playerPortraitField.style.width = 250f;
        playerPortraitField.RegisterValueChangedCallback(evt =>
        {
            asset.playerPortrait = evt.newValue as Sprite;
            EditorUtility.SetDirty(asset);
        });
        row.Add(playerPortraitField);

        Toolbar voiceRow = new Toolbar();

        ObjectField npcVoiceField = new ObjectField("Голос персонажа") { objectType = typeof(AudioClip), value = asset.npcVoice };
        npcVoiceField.style.width = 300f;
        npcVoiceField.RegisterValueChangedCallback(evt =>
        {
            asset.npcVoice = evt.newValue as AudioClip;
            EditorUtility.SetDirty(asset);
        });
        voiceRow.Add(npcVoiceField);

        ObjectField playerVoiceField = new ObjectField("Голос игрока") { objectType = typeof(AudioClip), value = asset.playerVoice };
        playerVoiceField.style.width = 300f;
        playerVoiceField.RegisterValueChangedCallback(evt =>
        {
            asset.playerVoice = evt.newValue as AudioClip;
            EditorUtility.SetDirty(asset);
        });
        voiceRow.Add(playerVoiceField);

        Toolbar backgroundRow = new Toolbar();

        ObjectField npcBackgroundField = new ObjectField("Фон окна персонажа") { objectType = typeof(Sprite), value = asset.npcWindowBackground };
        npcBackgroundField.style.width = 300f;
        npcBackgroundField.RegisterValueChangedCallback(evt =>
        {
            asset.npcWindowBackground = evt.newValue as Sprite;
            EditorUtility.SetDirty(asset);
        });
        backgroundRow.Add(npcBackgroundField);

        ObjectField playerBackgroundField = new ObjectField("Фон окна игрока") { objectType = typeof(Sprite), value = asset.playerWindowBackground };
        playerBackgroundField.style.width = 300f;
        playerBackgroundField.RegisterValueChangedCallback(evt =>
        {
            asset.playerWindowBackground = evt.newValue as Sprite;
            EditorUtility.SetDirty(asset);
        });
        backgroundRow.Add(playerBackgroundField);

        VisualElement column = new VisualElement();
        column.Add(row);
        column.Add(voiceRow);
        column.Add(backgroundRow);
        return column;
    }

    private void Save()
    {
        if (view == null || asset == null) return;
        view.SaveToAsset();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        ShowNotification(new GUIContent("Сохранено"), 0.7f);
    }

    private void OnDisable()
    {
        if (view == null || !view.Populated || asset == null) return;
        view.SaveToAsset();
        EditorUtility.SetDirty(asset);
    }
}

public class DialogueGraphView : GraphView
{
    public bool Populated { get; private set; }

    private DialogueAsset asset;
    private StartMarkerNode startNode;
    private readonly List<LineNode> lineNodes = new List<LineNode>();

    public DialogueGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports
            .Where(port => port.direction != startPort.direction && port.node != startPort.node)
            .ToList();
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.MenuItems().Clear();
        Vector2 position = LocalToGraphPosition(evt.localMousePosition);
        evt.menu.AppendAction("Добавить реплику", a => AddLineNode(position, NewLine()));
    }

    private Vector2 LocalToGraphPosition(Vector2 localPosition)
    {
        Vector3 result = viewTransform.matrix.inverse.MultiplyPoint3x4(localPosition);
        return new Vector2(result.x, result.y);
    }

    private static DialogueLine NewLine()
    {
        return new DialogueLine
        {
            text = "...",
            nextLineIndex = DialogueLine.AutoNextLine
        };
    }

    public void AddNewLineNode()
    {
        Rect center = contentRect;
        Vector2 position = LocalToGraphPosition(new Vector2(center.width * 0.5f, center.height * 0.5f));
        AddLineNode(position, NewLine());
    }

    private void AddLineNode(Vector2 position, DialogueLine line)
    {
        LineNode node = new LineNode(this, line, asset);
        node.SetPosition(new Rect(position, new Vector2(340f, 240f)));
        AddElement(node);
        lineNodes.Add(node);
    }

    public void RemoveEdge(Edge edge)
    {
        RemoveElement(edge);
    }

    public void Populate(DialogueAsset source)
    {
        DeleteElements(graphElements.ToList());
        startNode = null;
        lineNodes.Clear();
        asset = source;

        if (asset.lines.Count == 0)
        {
            asset.lines.Add(NewLine());
            asset.startIndex = 0;
        }

        for (int i = 0; i < asset.lines.Count; i++)
        {
            DialogueLine line = asset.lines[i];
            if (string.IsNullOrEmpty(line.nodeId)) line.nodeId = GUID.Generate().ToString();
            if (line.nodePosition == Vector2.zero)
            {
                line.nodePosition = new Vector2(160f + 380f * Mathf.FloorToInt(i / 8f), 60f + 260f * (i % 8));
            }
        }

        foreach (DialogueLine line in asset.lines)
        {
            AddLineNode(line.nodePosition, line);
        }

        startNode = new StartMarkerNode();
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        foreach (LineNode node in lineNodes)
        {
            Rect rect = node.GetPosition();
            if (rect.x < minX) minX = rect.x;
            if (rect.y < minY) minY = rect.y;
        }
        if (minX == float.MaxValue)
        {
            minX = 0f;
            minY = 0f;
        }
        startNode.SetPosition(new Rect(minX - 280f, minY + 40f, 150f, 50f));
        AddElement(startNode);

        for (int i = 0; i < asset.lines.Count && i < lineNodes.Count; i++)
        {
            DialogueLine line = asset.lines[i];
            LineNode node = lineNodes[i];

            int next = line.nextLineIndex;
            if (next == DialogueLine.AutoNextLine) next = i + 1;
            if (next >= 0 && next < lineNodes.Count)
            {
                Connect(node.NextOutput, lineNodes[next].Input);
            }

            IReadOnlyList<LineNode.ChoiceRow> rows = node.Rows;
            for (int c = 0; c < line.choices.Count && c < rows.Count; c++)
            {
                int target = line.choices[c].nextLineIndex;
                if (target >= 0 && target < lineNodes.Count)
                {
                    Connect(rows[c].Port, lineNodes[target].Input);
                }
            }
        }

        schedule.Execute(() => FrameAll());
        Populated = true;
    }

    private void Connect(Port output, Port input)
    {
        Edge edge = output.ConnectTo(input);
        AddElement(edge);
    }

    public void SaveToAsset()
    {
        if (!Populated || asset == null) return;

        List<LineNode> alive = lineNodes.Where(Contains).ToList();
        alive.Sort((a, b) => RankInAsset(a).CompareTo(RankInAsset(b)));

        Dictionary<LineNode, int> indexByNode = new Dictionary<LineNode, int>();
        for (int i = 0; i < alive.Count; i++) indexByNode[alive[i]] = i;

        foreach (LineNode node in alive)
        {
            DialogueLine line = node.Line;
            if (string.IsNullOrEmpty(line.nodeId)) line.nodeId = GUID.Generate().ToString();

            Rect rect = node.GetPosition();
            line.nodePosition = new Vector2(rect.x, rect.y);

            Edge nextEdge = node.NextOutput.connections.FirstOrDefault();
            line.nextLineIndex = ResolveTarget(nextEdge, indexByNode);

            for (int r = 0; r < node.Rows.Count; r++)
            {
                Edge choiceEdge = node.Rows[r].Port.connections.FirstOrDefault();
                node.LinesChoice(r).nextLineIndex = ResolveTarget(choiceEdge, indexByNode);
            }
        }

        asset.lines = alive.Select(n => n.Line).ToList();

        Edge startEdge = startNode != null && Contains(startNode)
            ? startNode.Output.connections.FirstOrDefault()
            : null;
        LineNode startTarget = startEdge != null ? startEdge.input.node as LineNode : null;
        if (startTarget != null && indexByNode.TryGetValue(startTarget, out int startIndex))
        {
            asset.startIndex = startIndex;
        }
        else
        {
            asset.startIndex = Mathf.Clamp(asset.startIndex, 0, Mathf.Max(0, asset.lines.Count - 1));
        }

        EditorUtility.SetDirty(asset);
    }

    private int RankInAsset(LineNode node)
    {
        int i = asset.lines.IndexOf(node.Line);
        return i >= 0 ? i : int.MaxValue;
    }

    private static int ResolveTarget(Edge edge, Dictionary<LineNode, int> indexByNode)
    {
        if (edge == null || edge.input == null || edge.input.node == null) return DialogueLine.EndDialogue;
        LineNode target = edge.input.node as LineNode;
        if (target == null || !indexByNode.TryGetValue(target, out int index)) return DialogueLine.EndDialogue;
        return index;
    }
}

public class LineNode : Node
{
    public class ChoiceRow
    {
        public DialogueChoice Choice;
        public Port Port;
        public VisualElement Root;
    }

    public DialogueLine Line { get; }
    public Port Input { get; }
    public Port NextOutput { get; }
    public IReadOnlyList<ChoiceRow> Rows => rows;

    private readonly DialogueGraphView owner;
    private readonly DialogueAsset asset;
    private readonly List<ChoiceRow> rows = new List<ChoiceRow>();

    public LineNode(DialogueGraphView ownerView, DialogueLine line, DialogueAsset sourceAsset)
    {
        owner = ownerView;
        Line = line;
        asset = sourceAsset;

        viewDataKey = line.nodeId;
        title = TitleText();
        expanded = true;

        Input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        Input.portName = "вход";
        inputContainer.Add(Input);

        Toggle playerToggle = new Toggle("Говорит игрок") { value = line.isPlayerSpeaking };
        playerToggle.RegisterValueChangedCallback(evt =>
        {
            line.isPlayerSpeaking = evt.newValue;
            title = TitleText();
        });
        mainContainer.Add(playerToggle);

        TextField textField = new TextField { value = line.text, multiline = true };
        textField.style.height = 90f;
        textField.style.flexGrow = 1f;
        textField.RegisterValueChangedCallback(evt => line.text = evt.newValue);
        mainContainer.Add(textField);

        Toggle endToggle = new Toggle("Закончить диалог на этой реплике") { value = line.endDialogueOnThisLine };
        endToggle.RegisterValueChangedCallback(evt => line.endDialogueOnThisLine = evt.newValue);
        mainContainer.Add(endToggle);

        Foldout portraits = new Foldout { text = "Портреты и имена", value = false };
        ObjectField npcPortrait = new ObjectField("Портрет персонажа") { objectType = typeof(Sprite), value = line.npcPortraitOverride };
        npcPortrait.RegisterValueChangedCallback(evt => line.npcPortraitOverride = evt.newValue as Sprite);
        TextField npcNameField = new TextField("Имя персонажа") { value = line.npcNameOverride ?? string.Empty };
        npcNameField.RegisterValueChangedCallback(evt =>
        {
            line.npcNameOverride = evt.newValue;
            title = TitleText();
        });
        ObjectField playerPortrait = new ObjectField("Портрет игрока") { objectType = typeof(Sprite), value = line.playerPortraitOverride };
        playerPortrait.RegisterValueChangedCallback(evt => line.playerPortraitOverride = evt.newValue as Sprite);
        TextField playerNameField = new TextField("Имя игрока") { value = line.playerNameOverride ?? string.Empty };
        playerNameField.RegisterValueChangedCallback(evt =>
        {
            line.playerNameOverride = evt.newValue;
            title = TitleText();
        });
        portraits.Add(npcPortrait);
        portraits.Add(npcNameField);
        portraits.Add(playerPortrait);
        portraits.Add(playerNameField);
        mainContainer.Add(portraits);

        Foldout voiceFoldout = new Foldout { text = "Голос", value = false };
        ObjectField npcVoice = new ObjectField("Голос персонажа") { objectType = typeof(AudioClip), value = line.npcVoiceOverride };
        npcVoice.RegisterValueChangedCallback(evt => line.npcVoiceOverride = evt.newValue as AudioClip);
        ObjectField playerVoice = new ObjectField("Голос игрока") { objectType = typeof(AudioClip), value = line.playerVoiceOverride };
        playerVoice.RegisterValueChangedCallback(evt => line.playerVoiceOverride = evt.newValue as AudioClip);
        voiceFoldout.Add(npcVoice);
        voiceFoldout.Add(playerVoice);
        mainContainer.Add(voiceFoldout);

        NextOutput = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        NextOutput.portName = "далее";
        outputContainer.Add(NextOutput);

        Button addChoiceButton = new Button(AddNewChoice) { text = "+ вариант" };
        extensionContainer.Add(addChoiceButton);
        foreach (DialogueChoice choice in line.choices.ToList())
        {
            BuildChoiceRow(choice);
        }

        RefreshExpandedState();
        RefreshPorts();
    }

    public DialogueChoice LinesChoice(int index)
    {
        return Line.choices[index];
    }

    private string TitleText()
    {
        if (Line.isPlayerSpeaking) return ResolveName(Line.playerNameOverride, asset.playerName);
        return ResolveName(Line.npcNameOverride, asset.npcName);
    }

    private static string ResolveName(string overrideName, string fallback)
    {
        return string.IsNullOrEmpty(overrideName) ? fallback : overrideName;
    }

    private void AddNewChoice()
    {
        DialogueChoice choice = new DialogueChoice { choiceText = "вариант", nextLineIndex = DialogueLine.EndDialogue };
        Line.choices.Add(choice);
        BuildChoiceRow(choice);
    }

    private void BuildChoiceRow(DialogueChoice choice)
    {
        ChoiceRow row = new ChoiceRow { Choice = choice };

        Port port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        port.portName = choice.choiceText;
        row.Port = port;

        VisualElement root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.alignItems = Align.Center;
        root.style.marginTop = 2f;

        TextField field = new TextField { value = choice.choiceText };
        field.style.flexGrow = 1f;
        field.RegisterValueChangedCallback(evt =>
        {
            choice.choiceText = evt.newValue;
            port.portName = evt.newValue;
        });

        Button removeButton = new Button(() => RemoveChoiceRow(row)) { text = "−" };

        root.Add(port);
        root.Add(field);
        root.Add(removeButton);

        outputContainer.Add(root);
        row.Root = root;
        rows.Add(row);

        RefreshPorts();
    }

    private void RemoveChoiceRow(ChoiceRow row)
    {
        foreach (Edge edge in row.Port.connections.ToList())
        {
            owner.RemoveEdge(edge);
        }
        row.Root.RemoveFromHierarchy();
        Line.choices.Remove(row.Choice);
        rows.Remove(row);
        RefreshPorts();
    }
}

public class StartMarkerNode : Node
{
    public Port Output { get; }

    public StartMarkerNode()
    {
        title = "СТАРТ";
        viewDataKey = "__start__";

        Output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        Output.portName = "";
        outputContainer.Add(Output);

        capabilities &= ~Capabilities.Deletable;
        mainContainer.style.backgroundColor = new Color(0.16f, 0.4f, 0.22f, 0.95f);
    }
}
