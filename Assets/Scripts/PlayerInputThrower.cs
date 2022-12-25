using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputThrower : MonoBehaviour
{
    public void Move(InputAction.CallbackContext context)
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<Player>().Move(context);
    }
}
