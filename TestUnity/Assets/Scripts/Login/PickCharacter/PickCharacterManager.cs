using ClientSproto;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PickCharacterManager : MonoBehaviour
{
    // 角色信息展示UI
    public GameObject CharacterShowCanvas;

    // 角色名对应的唯一ID
    Dictionary<string, Int64> Characters = new Dictionary<string, Int64> { };

    // 当前玩家选择的角色唯一ID
    Int64 CelectCharacterUUID;

    // Use this for initialization
    void Start () {
		foreach(var item in Login.Instance.CharacterList)
        {
            Characters[item.Value.name] = item.Key;
            NewCharacterShowCanvas(item.Value);
        }
	}
	
    void NewCharacterShowCanvas(character_overview character)
    {
        // 生成角色信息UI
        GameObject obj = Instantiate(CharacterShowCanvas);
        Image img = obj.GetComponentInChildren<Image>();
        Text nameText = img.transform.Find("NameText").GetComponent<Text>();
        Text jobText = img.transform.Find("JobText").GetComponent<Text>();
        Text sexText = img.transform.Find("SexText").GetComponent<Text>();
        nameText.text = character.name;
        jobText.text = character.job.ToString();
        sexText.text = character.sex.ToString();

        // 为UI添加点击事件
        UnityAction<BaseEventData> click = new UnityAction<BaseEventData>(SelectCharacter);
        EventTrigger.Entry myclick = new EventTrigger.Entry();
        myclick.eventID = EventTriggerType.PointerClick;
        myclick.callback.AddListener(click);
        EventTrigger et = img.GetComponentInChildren<EventTrigger>();
        et.triggers.Add(myclick);

        // 显示
        obj.SetActive(true);
    }

    void SelectCharacter(BaseEventData data)
    {
        GameObject obj = data.selectedObject;
        // 角色信息UI发生点击事件的时候,根据名称设置当前选择的角色唯一ID
        Image img = obj.GetComponentInChildren<Image>();
        Text nameText = img.transform.Find("NameText").GetComponent<Text>();
        CelectCharacterUUID = Characters[nameText.text];
    }

    public void EnterGame()
    {
        characterpick.request req = new characterpick.request();
        req.uuid = CelectCharacterUUID;
        NetSender.Send<ClientProtocol.characterpick>(req, (_) =>
        {
            characterpick.response rsp = _ as characterpick.response;
            Debug.Log("选择角色结果：" + rsp.ok);
            LoadScene();
        });
        Debug.Log("选择角色唯一ID：" + CelectCharacterUUID);
    }

    public void CreateCharacter()
    {
        SceneManager.LoadScene("CreateCharacter");
    }

    void LoadScene()
    {
        SceneManager.LoadScene("MainMap");
    }
}
