using ClientSproto;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateCharacterManager : MonoBehaviour {

    public InputField CharacterNameInputField;
    int job = 1;
    int sex = 1;

    public void CreateCharacter()
    {
        charactercreate.request req = new charactercreate.request();
        req.name = CharacterNameInputField.text;
        req.job = job;
        req.sex = sex;
        NetSender.Send<ClientProtocol.charactercreate>(req, (_) =>
        {
            Debug.Log("创建角色返回");
            Login.Instance.GetCharacterList();
        });

        Debug.Log("创建角色 名称：" + CharacterNameInputField.text + " 职业：" + job + " 性别：" + sex);
    }

    public void SetMale()
    {
        sex = 1;
    }

    public void SetFemale()
    {
        sex = 2;
    }

    public void SetJob1()
    {
        job = 1;
    }

    public void SetJob2()
    {
        job = 2;
    }

    public void SetJob3()
    {
        job = 3;
    }
}
