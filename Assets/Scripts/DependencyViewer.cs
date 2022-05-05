//------------------------------------------------------------
// Unity 工具集
// Copyright © 2020-2022 Yao Yilin. All rights reserved.
// 反馈: mailto:yaoyilin@sina.cn
//------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace UnityToolSet
{
    public class DependencyViewer : EditorWindow
    {
        [MenuItem("GameTools/Show Dependency")]
        public static void ShowDependency()
        {
            ShowWindow("");
        }

        public static DependencyViewer ShowWindow(string path)
        {
            DependencyViewer window = GetWindow(typeof(DependencyViewer), false, "资源依赖视图", true) as DependencyViewer;
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.wantsMouseEnterLeaveWindow = true;
            m_RootPath = path;
            window.Show();
            window.Refresh();
            return window;
        }

        private static string m_RootPath = string.Empty;

        private GraphView m_GraphView;

        private const float NODE_WIDTH = 230;
        private const float WINDOW_WIDTH = 1400;
        private const float WINDOW_HEIGHT = WINDOW_WIDTH * 0.618f;

        private bool m_GraphDrawn = false;
        private HashSet<string> m_RootDependences;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(WINDOW_WIDTH), GUILayout.Height(WINDOW_HEIGHT));
            {
                DrawDependency();
            }
            EditorGUILayout.EndHorizontal();

            if (PointerInWindow(Event.current.mousePosition) && DragAndDrop.paths.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (Event.current.type == EventType.DragExited)
                {
                    m_RootPath = DragAndDrop.paths[0];
                    Refresh();
                }
            }

            CommandEvent();
        }

        private bool PointerInWindow(Vector2 point)
        {
            return point.x > 0 && point.y > 0 && point.x < position.width && point.y < position.height;
        }

        private Label m_Title;
        private void OnEnable()
        {
            m_GraphView = new AssetGraphView { name = "资源依赖视图" };

            rootVisualElement.Add(m_GraphView);
            m_GraphView.style.position = Position.Absolute;
            m_GraphView.style.paddingLeft = 2;
            m_GraphView.style.paddingRight = 2;
            m_GraphView.style.paddingTop = 0;
            m_GraphView.style.paddingBottom = 2;
            m_GraphDrawn = false;

            VisualElement bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 0,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f),
                },
                tooltip = "滚动鼠标滚轮可以缩放窗口，按住滚轮可以拖动窗口。",
            };

            VisualElement options = new VisualElement
            {
                style = { alignContent = Align.Center }
            };

            bar.Add(options);
            bar.Add(new Button(SelectAsset) { text = "选择" });
            bar.Add(new Button(Refresh) { text = "重置", });
            bar.Add(new Button(ClearGraph) { text = "清除" });
            
            m_Title = new Label() { text = "", style = { alignContent = Align.Center, paddingLeft = 20, borderTopWidth = 0, fontSize = 20 } };

            bar.Add(m_Title);
            rootVisualElement.Add(bar);
            m_GraphView.StretchToParentSize();
            bar.BringToFront();
        }

        private void OnValueChanged(float value)
        {
            Debug.LogError(value);
            m_GraphView.viewTransform.scale = new Vector3(value, 1);
        }

        private void Refresh()
        {
            m_GraphDrawn = false;
        }

        private void OnDisable()
        {
            ClearGraph();
        }

        private void DrawDependency()
        {
            if (m_GraphDrawn || string.IsNullOrWhiteSpace(m_RootPath))
            {
                return;
            }

            ClearGraph();
            m_RootDependences = new HashSet<string>(AssetDatabase.GetDependencies(m_RootPath));
            s_Positions = new List<int>();

            foreach (string item in m_RootDependences)
            {
                DrawNode(item);
            }

            foreach (string item in m_RootDependences)
            {
                Connect(item);
            }

            SetPosition(m_RootPath, m_NodeMap[m_RootPath], 0);

            m_Title.text = m_RootPath;
            m_GraphDrawn = true;
        }

        private void SetPosition(string path, Node parent, int depth)
        {
            if (m_RootDependences.Count <= 0)
            {
                return;
            }

            if(!m_RootDependences.Remove(path))
            {
                return;
            }

            int index = GetIndex(depth);
            parent.SetPosition(new Rect(depth * NODE_WIDTH * 1.5f, 30 + index * NODE_WIDTH * 1.1f, 0, 0));
            SetIndex(depth);
            depth++;
            string[] dependencies = AssetDatabase.GetDependencies(path, false);
            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    Node node;
                    m_NodeMap.TryGetValue(dependencies[i], out node);
                    SetPosition(dependencies[i], node, depth);
                }
            }
        }

        private void DrawNode(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Node node = new Node
            {
                title = obj.name,
                style = { width = NODE_WIDTH }
            };

            node.extensionContainer.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);

            node.titleContainer.Add(new Button(() =>
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            })
            {
                style =
                {
                    height = 16.0f,
                    alignSelf = Align.Center,
                    alignItems = Align.Center
                },
                text = "选中"
            });

            VisualElement infoContainer = new VisualElement
            {
                style =
                {
                    paddingBottom = 4.0f,
                    paddingTop = 4.0f,
                    paddingLeft = 4.0f,
                    paddingRight = 4.0f
                }
            };

            infoContainer.Add(new Label
            {
                text = path,
                style = { whiteSpace = WhiteSpace.Normal }
            });

            node.extensionContainer.Add(infoContainer);
            Texture assetTexture = AssetPreview.GetAssetPreview(obj);
            if (!assetTexture)
            {
                assetTexture = AssetPreview.GetMiniThumbnail(obj);
            }

            if (assetTexture)
            {
                node.extensionContainer.Add(new Image
                {
                    image = assetTexture,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                {
                    paddingBottom = 4.0f,
                    paddingTop = 4.0f,
                    paddingLeft = 4.0f,
                    paddingRight = 4.0f
                }
                });
            }

            Port inPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Object));
            inPort.portName = "被其他引用";
            node.inputContainer.Add(inPort);

            Port outPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Object));
            outPort.portName = "依赖的资源";
            node.outputContainer.Add(outPort);
            node.RefreshPorts();

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.capabilities &= ~Capabilities.Deletable;

            m_GraphView.AddElement(node);
            m_AssetElements.Add(node);
            m_NodeMap.Add(path, node);
        }

        private void Connect(string path)
        {
            HashSet<string> dependences = new HashSet<string>(AssetDatabase.GetDependencies(path, false));
            Node parent;
            m_NodeMap.TryGetValue(path, out parent);
            foreach (string item in dependences)
            {
                Node root;
                m_NodeMap.TryGetValue(item, out root);
                Edge edge = new Edge
                {
                    input = root.inputContainer[0] as Port,
                    output = parent.outputContainer[0] as Port,
                };
                edge.input?.Connect(edge);
                edge.output?.Connect(edge);
                root.RefreshPorts();
                parent.RefreshPorts();
                m_GraphView.AddElement(edge);

                edge.capabilities &= ~Capabilities.Deletable;
                m_AssetElements.Add(edge);
            }
        }

        private static List<int> s_Positions;

        private int GetIndex(int depth)
        {
            if (depth == s_Positions.Count)
            {
                s_Positions.Add(0);
                return 0;
            }

            return s_Positions[depth];
        }

        private void SetIndex(int depth)
        {
            if (depth == s_Positions.Count)
            {
                s_Positions.Add(1);
            }
            else
            {
                s_Positions[depth]++;
            }
        }

        private void ClearGraph()
        {
            m_GraphView.DeleteElements(m_AssetElements);
            m_AssetElements.Clear();
            m_NodeMap.Clear();
        }

        private void SelectAsset()
        {
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, true, string.Empty, controlID);
        }

        void CommandEvent()
        {
            string commandName = Event.current.commandName;

            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                Debug.Log(commandName);
                GameObject go = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                if (go != null)
                {
                    string path = AssetDatabase.GetAssetPath(go);
                    m_RootPath = path;
                    Refresh();
                }
            }
        }
        
        private readonly List<GraphElement> m_AssetElements = new List<GraphElement>();
        private readonly Dictionary<string, Node> m_NodeMap = new Dictionary<string, Node>();

        public class AssetGraphView : GraphView
        {
            public AssetGraphView()
            {
                SetupZoom(ContentZoomer.DefaultMinScale, 1.5f);

                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());

                VisualElement background = new VisualElement
                {
                    style = { backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f) }
                };
                Insert(0, background);

                background.StretchToParentSize();
            }
        }
    }
}