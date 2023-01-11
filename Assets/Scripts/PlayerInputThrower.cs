using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerInputThrower : MonoBehaviour
{
    public void Move(InputAction.CallbackContext context)
    {
        //World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<Player>().Move(context);
    }

    public void Restart(InputAction.CallbackContext context)
    {
        if (context.canceled || context.started)
        {
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
