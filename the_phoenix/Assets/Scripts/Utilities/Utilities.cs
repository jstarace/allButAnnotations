using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using static UnityEditor.FilePathAttribute;
//using System.Numerics;
//using Unity.VisualScripting;


namespace Starace.Utils
{
    public static class Utilities
    {
        private static readonly Vector3 Vector3zero = Vector3.zero;
        private static readonly Vector3 Vector3one = Vector3.one;
        private static readonly Vector3 Vector3yDown = new Vector3(0f, -3f, 0f);
        public const int sortingOrderDefault = 5000;
        public const int cellSize = 3;

        public static void GetListXY(Vector3 currentPos, out int newX, out int newY)
        {
            newX = ((int)currentPos.x + 100) / cellSize;
            newY = ((int)currentPos.y + -50) / -cellSize;
        }

        public static void GetWorldXY(int x, int y, out Vector3 worldPos)
        {
            int tempX = (x * cellSize) - 100;
            int tempY = (y * -cellSize) + 50;
            worldPos = new Vector3(tempX, tempY, 0);
        }

        public static void GetMesh(Vector3 location, out Mesh newMesh)
        {
            newMesh = new Mesh();

            //Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[4];
            Vector2[] uv = new Vector2[4];
            int[] triangles = new int[6];

            float tx, ty, bx, by;

            tx = location.x - 0.5f;
            ty = location.y + 1.5f;
            bx = location.x + 2.5f;
            by = location.y - 1.5f;

            vertices[0] = new Vector3(tx, by, -1.0f);
            vertices[1] = new Vector3(tx, ty, -1.0f);
            vertices[2] = new Vector3(bx, ty, -1.0f);
            vertices[3] = new Vector3(bx, by, -1.0f);

            /*        vertices[0] = new Vector3(-100.5f, 48.5f, 0.0f);
                    vertices[1] = new Vector3(-100.5f, 51.5f, 0.0f);
                    vertices[2] = new Vector3(-97.5f, 51.5f, 0.0f);
                    vertices[3] = new Vector3(-97.5f, 48.5f, 0.0f);*/

            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(0, 1);
            uv[2] = new Vector2(1, 1);
            uv[3] = new Vector2(1, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 3;

            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.triangles = triangles;

        }


        /*
        public static TextMesh CreateWorldText (string text, Transform parent = null, Vector3 localPosition = default(Vector3), int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = sortingOrderDefault)
        {
            if (color == null) color = Color.white;
            return CreateWorldText(parent, text, localPosition, fontSize, (Color)color, textAnchor, textAlignment, sortingOrder);
        }

        public static TextMesh CreateWorldText(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment, int sortingOrder)
        {
            GameObject gameObject = new GameObject("World_Text " + text, typeof(TextMesh));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            textMesh.anchor = textAnchor;
            textMesh.alignment = textAlignment;
            textMesh.text = text;
            textMesh.font = (Font)Resources.Load<Font>("/Red_Hat_Mono/RedHatMono-VariableFont_wght");
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.lineSpacing = 1;
            textMesh.offsetZ = 0;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            transform.GetComponent<NetworkObject>().Spawn(true);
            return textMesh;
        }
        */
    }
}