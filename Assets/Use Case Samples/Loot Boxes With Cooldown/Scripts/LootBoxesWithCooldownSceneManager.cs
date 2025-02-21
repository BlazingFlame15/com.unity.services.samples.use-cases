﻿using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace UnityGamingServicesUseCases
{
    namespace LootBoxesWithCooldown
    {
        public class LootBoxesWithCooldownSceneManager : MonoBehaviour
        {
            public LootBoxesWithCooldownSampleView sceneView;

            int m_DefaultCooldownSeconds;

            int m_CooldownSeconds;


            async void Start()
            {
                try
                {
                    await UnityServices.InitializeAsync();

                    // Check that scene has not been unloaded while processing async wait to prevent throw.
                    if (this == null) return;

                    if (!AuthenticationService.Instance.IsSignedIn)
                    {
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                        if (this == null) return;
                    }

                    Debug.Log($"Player id:{AuthenticationService.Instance.PlayerId}");

                    await UpdateEconomy();
                    if (this == null) return;

                    await UpdateCooldownStatusFromCloudCode();
                    if (this == null) return;

                    Debug.Log("Initialization and signin complete.");

                    await WaitForCooldown();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            async Task UpdateCooldownStatusFromCloudCode()
            {
                try
                {
                    var cooldownResult = await CloudCodeManager.instance.CallGetStatusEndpoint();
                    if (this == null) return;

                    Debug.Log($"Retrieved cooldown flag:{cooldownResult.canGrantFlag} time:{cooldownResult.grantCooldown} default:{cooldownResult.defaultCooldown}");

                    m_DefaultCooldownSeconds = cooldownResult.defaultCooldown;
                    m_CooldownSeconds = cooldownResult.grantCooldown;
                }
                catch (CloudCodeResultUnavailableException)
                {
                    // Exception already handled by CloudCodeManager
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            async Task WaitForCooldown()
            {
                while (m_CooldownSeconds > 0)
                {
                    sceneView.UpdateCooldown(m_CooldownSeconds);

                    await Task.Delay(1000);
                    if (this == null) return;

                    m_CooldownSeconds--;
                }

                sceneView.UpdateCooldown(m_CooldownSeconds);
            }

            public async void GrantTimedRandomReward()
            {
                try
                {
                    sceneView.OnClaimingLootBoxes();

                    var grantResult = await CloudCodeManager.instance.CallClaimEndpoint();
                    if (this == null) return;

                    Debug.Log($"CloudCode script rewarded: {grantResult}");

                    await UpdateEconomy();
                    if (this == null) return;

                    m_CooldownSeconds = m_DefaultCooldownSeconds;

                    await WaitForCooldown();
                }
                catch (CloudCodeResultUnavailableException)
                {
                    // Exception already handled by CloudCodeManager
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            async Task UpdateEconomy()
            {
                await Task.WhenAll(EconomyManager.instance.RefreshCurrencyBalances(),
                    EconomyManager.instance.RefreshInventory());
            }
        }
    }
}
