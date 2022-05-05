//------------------------------------------------------------
// Unity 工具集
// Copyright © 2020-2022 Yao Yilin. All rights reserved.
// 反馈: mailto:yaoyilin@sina.cn
//------------------------------------------------------------

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityToolSet
{
    public class AudioTrimmer : EditorWindow
    {
        [MenuItem("GameTools/Show Audio Trimmer")]
        private static void ShowUserDataWindow()
        {
            AudioTrimmer window = (AudioTrimmer) GetWindow(typeof(AudioTrimmer), false, "Audio Trimmer");
            window.maxSize = new Vector2(800, 230);
            window.minSize = window.maxSize;
            window.Show();
        }

        private string[] _assets;
        private bool[] _toggles;

        private float _trimThreshold = 0.1f;
        private float _lastThreshold = 0.1f;

        private void OnEnable()
        {
            _assets = AssetDatabase.FindAssets("t:AudioClip");
            _toggles = new bool[_assets.Length];
            if (_toggles.Length > 0)
            {
                _toggles[0] = true;
            }
        }

        private Vector2 _currentPosition = new Vector2();
        private int _current;

        private void OnDestroy()
        {
            var assets = AssetDatabase.FindAssets("t:AudioClip");
            int i = assets.Length - 1;
            while (i >= 0)
            {
                var asset = AssetDatabase.GUIDToAssetPath(assets[i]);
                if (Path.GetFileNameWithoutExtension(asset).EndsWith("_trimmed"))
                {
                    AssetDatabase.DeleteAsset(asset);
                }

                i--;
            }
        }

        AudioClip _trimmed = null;

        private void OnGUI()
        {
            AudioClip current = null;

            GUILayout.BeginHorizontal("box");
            {
                _currentPosition = GUILayout.BeginScrollView(_currentPosition, GUILayout.Width(200), GUILayout.Height(300));
                {
                    for (int i = 0; i < _assets.Length; i++)
                    {
                        var asset = _assets[i];
                        string path = AssetDatabase.GUIDToAssetPath(asset);
                        string name = Path.GetFileNameWithoutExtension(path);
                        _toggles[i] = GUILayout.Toggle(_toggles[i], name);
                        if (_toggles[i] && _current != i)
                        {
                            _toggles[_current] = false;
                            AssetDatabase.DeleteAsset(GetTrimmedName(AssetDatabase.GUIDToAssetPath(_assets[_current])));
                            _current = i;
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal("box", GUILayout.Width(128 * 2 + 50));
                    {
                        string path = AssetDatabase.GUIDToAssetPath(_assets[_current]);
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        current = clip;
                        GUILayout.BeginVertical("box", GUILayout.Width(128));
                        {
                            DrawClip(clip);
                        }
                        GUILayout.EndVertical();

                        GUILayout.Space(20);
                        GUILayout.Label("\n\n\n\n\n>>>");
                        GUILayout.Space(20);

                        GUILayout.BeginVertical("box", GUILayout.Width(128));
                        {
                            string newPath = GetTrimmedName(path);
                            string savePath = GetFullPath(newPath);
                            if (clip.loadType == AudioClipLoadType.DecompressOnLoad)
                            {
                                if (!File.Exists(savePath))
                                {
                                    var newClip = TrimClip(clip);
                                    Save(savePath, newClip);
                                    AssetDatabase.Refresh();
                                    _trimmed = newClip;
                                }

                                DrawClip(AssetDatabase.LoadAssetAtPath<AudioClip>(newPath));
                            }
                            else
                            {
                                _trimmed = null;
                                DrawClip(null);
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();


                GUILayout.BeginVertical("box", GUILayout.Width(200));
                {
                    GUILayout.Label($"Threshold: {_trimThreshold}");
                    _trimThreshold = GUILayout.HorizontalSlider(_trimThreshold, 0, 1f);
                    if (Math.Abs(_lastThreshold - _trimThreshold) > 0)
                    {
                        string newPath = GetTrimmedName(AssetDatabase.GUIDToAssetPath(_assets[_current]));
                        string savePath = GetFullPath(newPath);
                        File.Delete(savePath);
                        _lastThreshold = _trimThreshold;
                    }

                    GUILayout.Space(30);

                    if (_trimmed != null)
                    {
                        float res = 1 - _trimmed.length / current.length;
                        GUILayout.Label($"trimmed: {res * 100:F}%");
                    }
                    else
                    {
                        GUILayout.Label("Trimmed: 0 %");
                    }

                    GUILayout.Space(10);

                    if (GUILayout.Button("Trim"))
                    {
                        string savePath = GetFullPath(AssetDatabase.GUIDToAssetPath(_assets[_current]));
                        File.Delete(savePath);
                        Save(savePath, _trimmed);
                        AssetDatabase.Refresh();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private string GetFullPath(string unityPath)
        {
            return Path.Combine(Application.dataPath, unityPath.Remove(0, 7));
        }

        private string GetTrimmedName(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            string newPath = Path.Combine(dir, $"{name}_trimmed{ext}");
            return newPath;
        }

        private void DrawClip(AudioClip clip)
        {
            if (clip != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(clip);
                if (preview == null || AssetPreview.IsLoadingAssetPreview(clip.GetInstanceID()))
                {
                    GUILayout.Box("loading...", GUILayout.Width(128), GUILayout.Height(128));
                }
                else
                {
                    GUILayout.Box(preview, GUILayout.Width(preview.width), GUILayout.Height(preview.height));
                }

                GUILayout.Label($"name:{clip.name ?? "-"}", GUILayout.Width(128));
                GUILayout.Label($"time:{clip.length} s", GUILayout.Width(128));
                GUILayout.Label($"frequency:{clip.frequency} hz", GUILayout.Width(128));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(49);
                    if (GUILayout.Button("▶️", GUILayout.Width(30)))
                    {
                        PlayClip(clip);
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Box("not support...", GUILayout.Width(128), GUILayout.Height(128));
                GUILayout.Label("name:-", GUILayout.Width(128));
                GUILayout.Label("time:0 s", GUILayout.Width(128));
                GUILayout.Label("frequency:0 hz", GUILayout.Width(128));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(49);
                    if (GUILayout.Button("✖️️", GUILayout.Width(30)))
                    {
                    }
                }
            }
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] {typeof(AudioClip), typeof(int), typeof(bool)},
                null
            );
            method.Invoke(
                null,
                new object[] {clip, startSample, loop}
            );
        }

        const int HEADER_SIZE = 44;

        /// <summary>
        /// https://gist.githubusercontent.com/darktable/2317063/raw/b7d3513ae09e075c79826395f138f93777eac222/SavWav.cs
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static bool Save(string filepath, AudioClip clip)
        {
            if (File.Exists(filepath))
            {
                return true;
            }

            Debug.Log(filepath);

            try
            {
                using (var fileStream = CreateEmpty(filepath))
                {
                    ConvertAndWrite(fileStream, clip);

                    WriteHeader(fileStream, clip);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            return true;
        }

        private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            //converting in 2 float[] steps to short[], //then short[] to byte[]

            byte[] bytesData = new byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short) (samples[i] * rescaleFactor);
                byte[] byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        private static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        private AudioClip TrimClip(AudioClip clip)
        {
            float threshold = _trimThreshold / 100.0f;
            float[] samples = new float[clip.samples];
            if (clip.GetData(samples, 0))
            {
                int i = 0;
                for (; i < samples.Length; i++)
                {
                    if (Mathf.Abs(samples[i]) >= threshold)
                    {
                        break;
                    }
                }

                int j = samples.Length - 1;
                for (; j >= 0; j--)
                {
                    if (Mathf.Abs(samples[j]) >= threshold)
                    {
                        break;
                    }
                }

                float[] newSamples = new float[j - i + 1];
                Array.Copy(samples, i, newSamples, 0, newSamples.Length);
                AudioClip newAudioClip = AudioClip.Create(clip.name, newSamples.Length, clip.channels, clip.frequency, false);
                newAudioClip.SetData(newSamples, 0);
                return newAudioClip;
            }

            return null;
        }

        private static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            ushort two = 2;
            ushort one = 1;

            byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort) (channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bps = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }
    }
}