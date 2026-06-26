using UnityEngine;
using System.Collections.Generic;

public class HierarchyFlattener : MonoBehaviour
{
    [Header("Objetos Padre a aplanar (Ej: Calles)")]
    [Tooltip("Añade aquí los GameObjects cuyos hijos directos quieres soltar en la raíz al iniciar.")]
    public List<Transform> parentsToFlatten;

    void Awake()
    {
        // Se ejecuta al instante al cargar la escena, antes del Start()
        //FlattenHierarchies();
    }

    public void FlattenHierarchies()
    {
        // Recorremos cada padre de la lista
        foreach (Transform parentObj in parentsToFlatten)
        {
            // Protección por si dejas un hueco vacío en la lista
            if (parentObj == null) continue;

            // Recorremos los hijos HACIA ATRÁS para no romper los índices al desvincularlos
            for (int i = parentObj.childCount - 1; i >= 0; i--)
            {
                Transform child = parentObj.GetChild(i);

                // SetParent(null) mueve el objeto directamente a la raíz (nivel 0) de la escena
                child.SetParent(null);
            }
        }
    }
}