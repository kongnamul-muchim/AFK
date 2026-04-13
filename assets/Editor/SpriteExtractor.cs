using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteExtractor
{
    [MenuItem("Assets/Extract Selected Sprites to PNG")]
    public static void ExtractSprites()
    {
        // 선택한 파일들 가져오기
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            // 해당 경로의 모든 하위 스프라이트 로드
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    Extract(sprite);
                }
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("모든 스프라이트 추출이 완료되었습니다!");
    }

    private static void Extract(Sprite sprite)
    {
        Texture2D sourceTex = sprite.texture;
        
        // 스프라이트의 영역만큼 새로운 텍스처 생성
        Texture2D newTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        
        // 원본에서 해당 영역의 픽셀만 복사
        Color[] pixels = sourceTex.GetPixels(
            (int)sprite.textureRect.x, 
            (int)sprite.textureRect.y, 
            (int)sprite.textureRect.width, 
            (int)sprite.textureRect.height);
            
        newTex.SetPixels(pixels);
        newTex.Apply();

        // PNG로 변환 및 저장
        byte[] bytes = newTex.EncodeToPNG();
        string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceTex));
        string savePath = Path.Combine(directory, sprite.name + ".png");
        
        File.WriteAllBytes(savePath, bytes);
        Object.DestroyImmediate(newTex);
    }
}