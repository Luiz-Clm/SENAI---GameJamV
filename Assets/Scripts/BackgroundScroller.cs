using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [System.Serializable]
    public class BackgroundLayer
    {
        public Transform transform;
        public float scrollSpeed = 2f;
        public float resetPositionX = 20f;   // Posição x quando o layer deve ser resetado
        public float startPositionX = 20f;   // Posição x para onde o layer vai retornar
    }

    [SerializeField] private BackgroundLayer[] layers;

    void Update()
    {
        foreach (var layer in layers)
        {
            // Move o background para a esquerda
            layer.transform.Translate(Vector2.left * layer.scrollSpeed * Time.deltaTime);
            
            // Reseta a posição para criar o efeito de rolagem infinita (parallax)
            if (layer.transform.position.x <= -layer.resetPositionX)
            {
                Vector3 pos = layer.transform.position;
                pos.x = layer.startPositionX;
                layer.transform.position = pos;
            }
        }
    }
}
