using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
using static CoreLite.CoreIapH5Mgr;

namespace CoreLite
{
    public class CoreIapNativeMgr : CoreSingleton<CoreIapNativeMgr>,IDetailedStoreListener
    {
        public Dictionary<string, ProductType> ProductDic = new Dictionary<string, ProductType>()
        {

        };
        #region 对外接口
        public void BuyProductByID(CoreLiteBuy.Request req)
        {

#if UNITY_EDITOR

            var res = new CoreLitePurchase.Result(req.create_order_request,CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(null),true, false ,req.payment_type);
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
#else 
            
            if (req.payment_type == CoreLitePaymentType.H5)
            {
                Application.OpenURL(req.h5_url);
                CoreIapH5Mgr.Instance.purchasePending = true;
            }
            else
            {
                mProductName = req.create_order_request.product_name;
                mGameExtraParam = req.create_order_request.game_extra_param;
                Product m_p = GetProductInfoByID(req.create_order_request.product_id);
                var currencyCode = "";
                var localizedPrice = "";
                if (m_p != null)
                {
                    currencyCode = m_p.metadata.isoCurrencyCode;
                    localizedPrice = m_p.metadata.localizedPrice.ToString();
                }

                // 设置监听购买二次验证的回调
                if (IsInitialized())
                {
                    if (m_PurchaseInProgress)
                    {
                        CoreLogUtils.Debug("[iap] The payment is in progress, please do not initiate the payment repeatedly.");
                        return;
                    }

                    Product product = m_StoreController.products.WithID(req.create_order_request.product_id);
                    if (product != null && product.availableToPurchase)
                    {
                        OnPurchaseBegin?.Invoke();
                        m_PurchaseInProgress = true;
                        CoreLogUtils.Debug(
                            string.Format("[iap] Purchasing product asychronously: '{0}'", product.definition.id));
                        
                        if (googlePlayConfiguration != null)
                        {
                            if (!string.IsNullOrEmpty(req.create_order_request.product_name))
                            {
                                var data = req.create_order_request.product_name +"|"+req.create_order_request.product_desc+"|"+req.create_order_request.quantity.ToString()+"|"+req.order_Id+"|"+req.create_order_request.game_server_channel;
                                googlePlayConfiguration.SetObfuscatedAccountId(data);
                                CoreLogUtils.Debug("set obfuscatedAccountId:"+data);
                            }
                            googlePlayConfiguration.SetObfuscatedProfileId(req.create_order_request.game_extra_param);
                        }
#if UNITY_IOS
                        // 点击购买时存储数据，成功udid
                        var sActorID = CoreIapIosOrderMgr.GenerateGuid();
                        var dataDict = new Dictionary<string, string>
                        {
                            { "product_name", req.create_order_request.product_name ?? "" },
                            { "product_desc", req.create_order_request.product_desc ?? ""},
                            { "qty", req.create_order_request.quantity.ToString() },
                            { "gameServerChannel", req.create_order_request.game_server_channel ?? "" },
                            { "cacheOrderId", req.order_Id ?? "" },
                            { "actorID", sActorID }
                        };
                        CoreLogUtils.Debug("set actorID:"+sActorID+" productName:"+req.create_order_request.product_name+" productDesc:"+req.create_order_request.product_desc+" qty:"+req.create_order_request.quantity+" gameServerChannel:"+req.create_order_request.game_server_channel+" orderId:"+req.order_Id);
                        CoreIapIosOrderMgr.SaveOrder(dataDict);
                        m_StoreExtensionProvider.GetExtension<IAppleExtensions>().SetApplicationUsername(sActorID);
#endif
                        m_StoreController.InitiatePurchase(product);
                    }
                    else
                    {
                        CoreIapEvents.IAPFail(mProductName,
                            req.create_order_request.product_id, 
                            "", 
                            "", 
                            "",
                            "", 
                            mGameExtraParam, 
                            "BuyProductID FAIL. Not purchasing product, either is not found or is not available for purchase",
                            #if UNITY_ANDROID
                            "googleplay",
                            #else
                            "appstore",
                            #endif
                            0
                            );

                        CoreLogUtils.Debug("[iap] BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                    }     
                }
                else
                {
                    
                  
                    var res = new CoreLitePurchase.Result(CoreIapH5Mgr.Instance.currentOrderReq,CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.NativeApp);
                    CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
                    CoreIapEvents.IAPFail(mProductName,req.create_order_request.product_id,
                        "", 
                        "", 
                        "",
                        "", 
                        mGameExtraParam, 
                        "BuyProductID FAIL. IAP Not initialized or Not add product.",
#if UNITY_ANDROID
                        "googleplay",
#else
                        "appstore",
#endif
                        0
                    );
                    CoreLogUtils.Debug("[iap] BuyProductID FAIL. IAP Not initialized or Not add product.");
                }
        }
#endif
        }

        public void DoConfirmPendingPurchaseByID(string productId)
        {
            if (m_StoreController == null && m_StoreExtensionProvider == null) return;
            Product product = m_StoreController.products.WithID(productId);
            if (null == product) return;

            if (string.IsNullOrEmpty(product.transactionID)) return;
            if (product != null && product.availableToPurchase)
            {
                m_StoreController.ConfirmPendingPurchase(product);
                m_PurchaseInProgress = false;
            }
        }

        public string GetPriceByID(string pID)
        {
            if (m_StoreController == null && m_StoreExtensionProvider == null)
                return "";

            Product[] tProducts = m_StoreController.products.all;
            for (int i = 0; i < tProducts.Length; i++)
            {
                Product tItem = tProducts[i];
                if (tItem.definition.id.Equals(pID))
                {
#if UNITY_ANDROID
                    return tItem.metadata.GetGoogleProductMetadata().localizedPriceString;
#else
                    return tItem.metadata.localizedPriceString;
#endif
                }
            }

            return "";
        }

        public Product GetProductInfoByID(string pID)
        {
            if (m_StoreController == null && m_StoreExtensionProvider == null)
                return null;
            for (int i = 0; i < m_StoreController.products.all.Length; i++)
            {
                Product tItem = m_StoreController.products.all[i];
                if (tItem.definition.id.Equals(pID))
                {
                    return tItem;
                }
            }

            return null;
        }
        #endregion

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            m_PurchaseInProgress = false;
            var wrapper = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.purchasedProduct.receipt);
            var store = (string)wrapper["Store"];
            var payload = (string)wrapper["Payload"];
            var profileId = mGameExtraParam;
            var productName = "";
            var proDesc = "";
            var qty = "";
            var cacheOrderId = "";
            var gameServerChannel = "";

#if UNITY_ANDROID
            var payloadObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
            var o = (string)payloadObj["json"];

            var payloadData = JsonConvert.DeserializeObject<Dictionary<string, object>>(o);

            if (payloadData.TryGetValue("obfuscatedAccountId", out var obfuscatedAccountIdValue))
            {
                var obfuscatedAccountId = (string)obfuscatedAccountIdValue;
                if (!string.IsNullOrEmpty(obfuscatedAccountId))
                {
                    var obfuscatedAccountIdArr = obfuscatedAccountId.Split('|');
                    productName = obfuscatedAccountIdArr[0];
                    proDesc = obfuscatedAccountIdArr[1];
                    qty = obfuscatedAccountIdArr[2];
                    cacheOrderId = obfuscatedAccountIdArr[3];
                    gameServerChannel = obfuscatedAccountIdArr[4];
                }

                CoreLogUtils.Debug("[iap] ProcessPurchase data:"+productName+ " proDesc:"+proDesc+"qty:"+ qty+" orderId:"+cacheOrderId+" gameServerChannel:"+gameServerChannel+ "data:"+obfuscatedAccountId);

            }
            

            if (payloadData.TryGetValue("obfuscatedProfileId", out var value))
            {
                var obfuscatedProfileId = (string)value;
                if (!string.IsNullOrEmpty(obfuscatedProfileId))
                {
                    profileId = obfuscatedProfileId;
                }
            }
            CoreLogUtils.Debug("[iap] productName"+productName+ " profileId:"+profileId);
#endif

            string token;
            string orderId;
            if (Application.platform == RuntimePlatform.Android)
            {
                var gpDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                var gpJson = (string)gpDetails["json"];
                var tokenJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(gpJson);
                token = (string)tokenJson["purchaseToken"];
                orderId = (string)tokenJson["orderId"];

                CoreLogUtils.Debug("[iap] ClientIAPSuccess productId : " + e.purchasedProduct.definition.id +
                                    " , transactionID : " + orderId + " , token : " + token + " , localizedPrice : " +
                                    e.purchasedProduct.metadata.localizedPriceString + " , isoCurrencyCode : " +
                                    e.purchasedProduct.metadata.isoCurrencyCode);
            }
            else
            {
                token = "";
                orderId = (string)wrapper["TransactionID"];
                CoreLogUtils.Debug("[iap] ClientIAPSuccess productId : " + e.purchasedProduct.definition.id +
                                    " , transactionID : " + orderId + " , token : " + token + " , localizedPrice : " +
                                    e.purchasedProduct.metadata.localizedPriceString + " , isoCurrencyCode : " +
                                    e.purchasedProduct.metadata.isoCurrencyCode + " localizedDescription:" + e.purchasedProduct.metadata.localizedDescription + " title:" + e.purchasedProduct.metadata.localizedTitle);
            }

#if UNITY_IOS
            productName = string.IsNullOrEmpty(e.purchasedProduct.metadata.localizedTitle) ? "" : e.purchasedProduct.metadata.localizedTitle;
            cacheOrderId = CoreIapH5Mgr.Instance.currentOrderId ?? "";
            qty = CoreIapH5Mgr.Instance.currentOrderReq?.quantity.ToString();
            proDesc = CoreIapH5Mgr.Instance.currentOrderReq?.product_desc ?? "";
            gameServerChannel = CoreIapH5Mgr.Instance.currentOrderReq?.game_server_channel ?? "";

#endif
            DataConfig newData = new DataConfig();
            newData.msg = new Dictionary<string, string>
            {
                { "price", e.purchasedProduct.metadata.localizedPrice.ToString() },
                { "product_desc", proDesc },
                { "qty", qty },
                { "product_id", e.purchasedProduct.definition.id },
                { "iap_name", (string)productName },
                { "purchase_token", token },
                { "order_no", cacheOrderId },
                { "order_id", orderId },
                { "currency", e.purchasedProduct.metadata.isoCurrencyCode },
#if UNITY_ANDROID
                { "payment_method", "googleplay" },
#else
                { "payment_method", "appstore" },
#endif
                { "product_type", e.purchasedProduct.definition.type.ToString() },
                { "mGameExtraParam", (string)profileId },
                { "game_server_channel",gameServerChannel }
            };
            

            newData.state = DataState.def;

            // 客户端购买完成方法上报
            CoreIapEvents.IAPClientFinish((string)productName, e.purchasedProduct.definition.id, orderId,cacheOrderId,
                e.purchasedProduct.metadata.isoCurrencyCode, e.purchasedProduct.metadata.localizedPrice.ToString(),
                mGameExtraParam, e.purchasedProduct.definition.type.ToString());

            // iOS 点击恢复购买后 订阅商品不通过恢复购买回复，非消耗品通过恢复购买回调恢复
            if (mIsRestoreClick && e.purchasedProduct.definition.type == ProductType.Subscription)
            {

            }
            else
            {
                // 保存订单信息，防止掉单
                SaveOrder(newData);
                // 校验购买订单
                VerifyPurchaseII(newData);
            }
            return PurchaseProcessingResult.Complete;
        }


        private List<int> NotRetryCode = new List<int> { 0, 430, 900, 901, 902, 903, 904, 905 };
        private void VerifyPurchaseII(DataConfig args)
        {
            CoreLogUtils.Debug("[iap] Start VerifyPurchaseII orderid:" + args.msg["order_no"] + "status:" + args.state);
            if (args.state != DataState.def) return;
            
            // 记录订单处理次数，处理5次还是失败就不处理了
#if UNITY_ANDROID            
            var orderId = args.msg["order_no"];
#else
            var orderId = args.msg["order_id"];
#endif            
            mRepeatCountDic[orderId] = mRepeatCountDic.GetValueOrDefault(orderId) + 1;

            // 开始处理,设置正在处理的状态
            SetOrderStatue(args, DataState.deal);

            var gameExtraParam = args.msg.GetValueOrDefault("mGameExtraParam")?.ToString() ?? mGameExtraParam;

            // Android/iOS平台才校验
#if (UNITY_IOS || UNITY_ANDROID) 
            CoreServerMgr.Instance.IapReport(args.msg.ToDictionary(kv => kv.Key, kv => (object)kv.Value), (code, msg, data) =>
            {
                CoreLogUtils.Debug("[iap] IapReport callback code :"+code+""+msg+""+data+ " game purchase:"+CoreSdkMgr.OnPurchaseComplete);

#if UNITY_IOS
                try
                {
                    var localData = CoreIapIosOrderMgr.GetOrder(data.appstore_account_token);
                    if (localData != null)
                    {
                        args.msg["order_no"] = localData["cacheOrderId"].ToString();
                        args.msg["product_name"] = localData["product_name"].ToString();
                        args.msg["product_desc"] = localData["product_desc"].ToString();
                        args.msg["qty"] = localData["qty"].ToString();
                        args.msg["game_server_channel"] = localData["gameServerChannel"].ToString();
                    }
                }
                catch
                {
                    
                }
#endif

                if ((code == 0 || code == 430) && data.illegal_order == 0)
                {
                        CoreIapEvents.IAPSuccess(args.msg["iap_name"], 
                            args.msg["product_id"].ToString(), 
                            args.msg["order_no"].ToString(), 
                            args.msg["order_id"].ToString(), 
                            args.msg["currency"].ToString(), 
                            args.msg["price"].ToString(), 
                            gameExtraParam, 
                            args.msg["product_type".ToString()], 
                            "production",
                            false,
#if UNITY_ANDROID
                            "Goolge"
#else
                            "Apple"
#endif                  

                        );
                    
                    var res = new CoreLitePurchase.Result(GetCreateOrderInfo(args.msg),GetQueryOrderInfo(args.msg),true, false ,CoreLitePaymentType.NativeApp);
                    CoreSdkMgr.OnPurchaseComplete?.Invoke(res);

                }else
                {
                    if (NotRetryCode.Contains(code))
                    {
#if UNITY_ANDROID                        
                        RemoveVerifyFailOrderId(args.msg["order_no"]);
#else
                        RemoveVerifyFailOrderId(args.msg["order_id"]);
#endif      
                    }
                    else
                    {
                        SaveVerifyFailOrderId(args);
                    }

                    var resRequest = new CoreLiteCreateOrder.Request(args.msg["iap_name"],args.msg["product_id"].ToString(),args.msg["product_desc"].ToString(),float.Parse(args.msg["price"]),int.Parse(args.msg["qty"]),args.msg["currency"].ToString(),gameExtraParam,args.msg["game_server_channel"].ToString());
                    var res = new CoreLitePurchase.Result(resRequest,CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.NativeApp);
                    CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
                }
            });
            
#endif
        }
        // 原生请求里有些数据没有
        private CoreLiteQueryOrder.Result GetQueryOrderInfo(Dictionary<string, string> args)
        {
            var result = CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(GetCreateOrderInfo(args));
                        result.order_no = args["order_no"].ToString();
                        result.product_name = args["iap_name"].ToString();
                        result.product_id = args["product_id"].ToString();
                        result.transaction_id = args["order_id"].ToString();

            return result;
        }
        private CoreLiteCreateOrder.Request GetCreateOrderInfo(Dictionary<string, string> args)
        {
            if (CoreIapH5Mgr.Instance.currentOrderReq != null) return CoreIapH5Mgr.Instance.currentOrderReq;
        
            float.TryParse(args["price"]?.ToString(), out float fPrice);
            int.TryParse(args["qty"]?.ToString(), out int qty);
            
            return new CoreLiteCreateOrder.Request(
                args["iap_name"].ToString(),
                args["product_id"].ToString(),
                args["product_desc"].ToString(),
                fPrice,
                qty == 0 ? 1 : qty, // 保持默认值为1
                args["currency"].ToString(),
                args["mGameExtraParam"].ToString(),
                args["game_server_channel"].ToString()
            );
        }

        private static string _purchaseOrderMap = "PurchaseOrderMap";
        #region 本地订单处理逻辑
        private void ResetSaveOrderStatus()
        {
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                try
                {
                    List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
                    if (list.Count > 0)
                    {
                        foreach (var data in list)
                        {
                            data.state = DataState.def;
                        }

                        ES3.Save(_purchaseOrderMap, list);

                    }
                }
                catch (System.Exception)
                {
                    ES3.DeleteKey(_purchaseOrderMap);
                }

            }
        }

        // 初始化iap成功后/购买失败后/购买成功
        public void ProcessLocalOrder()
        {
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
                CoreLogUtils.Debug("[iap] IAP ProcessLocalOrder 读取失败订单列表，count：" + list.Count + " mRepeatCountDic" + mRepeatCountDic.Count);
                if (list.Count > 0)
                {
                    // 当前进程每条订单校验5次，如果失败则不再处理

                    // 重复校验时判断次数是否大于5，大于5则不再处理
                    DataConfig tempData = null;
                    if (mRepeatCountDic.Count > 0)
                    {
                        foreach (var data in list)
                        {
#if UNITY_ANDROID
                            if (mRepeatCountDic.ContainsKey(data.msg["order_no"]) && mRepeatCountDic[data.msg["order_no"]] <= 5)
#else
                            if (mRepeatCountDic.ContainsKey(data.msg["order_id"]) && mRepeatCountDic[data.msg["order_id"]] <= 5)
#endif
                            {
                                tempData = data;
                                break;
                            }
                        }
                    }
                    // 如果还有次数不超过5次的订单，则继续校验，否则就不再处理
                    if (tempData != null)
                    {
                        VerifyPurchaseII(tempData);
                        CoreIapEvents.IAPCacheOrder(tempData.msg);
                    }
                    else
                    {
                        CancelInvoke(nameof(ProcessLocalOrder));
                        inDealFailOrder = false;
                    }
                }
                else
                {
                    CancelInvoke(nameof(ProcessLocalOrder));
                    inDealFailOrder = false;
                }
            }
        }

        // 设置订单状态-正在处理/未处理
        private void SetOrderStatue(DataConfig _productInfo, DataState state)
        {
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
#if UNITY_ANDROID
                var value = _productInfo.msg["order_no"];
#else           
                var value = _productInfo.msg["order_id"];
#endif
                var want = list.Find(e => e.msg.ContainsValue(value));
                if (want != null)
                {
                    want.state = state;
                    ES3.Save(_purchaseOrderMap, list);
                }
            }
        }


        // 购买后先存储订单，再校验订单
        private void SaveOrder(DataConfig _productInfo)
        {
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
#if UNITY_ANDROID
                var value = _productInfo.msg["order_no"];
#else           
                var value = _productInfo.msg["order_id"];
#endif
                var want = list.Find(e => e.msg.ContainsValue(value));
                if (want == null)
                {
                    list.Add(_productInfo);
                    ES3.Save(_purchaseOrderMap, list);
                    CoreIapEvents.SDKSaveOrder(_productInfo.msg);
                }
            }
            else
            {
                CoreAnalyticsMgr.TrackEvent("IAP_Save_Order_Start");

                List<DataConfig> tem = new List<DataConfig>
                {
                    _productInfo
                };
                ES3.Save(_purchaseOrderMap, tem);
                CoreAnalyticsMgr.TrackEvent("IAP_Save_Order_End");
                CoreIapEvents.SDKSaveOrder(_productInfo.msg);
            }
        }


        private bool inDealFailOrder;

        // 保存校验失败的订单
        private void SaveVerifyFailOrderId(DataConfig _productInfo)
        {
            CoreLogUtils.Debug("[iap] iap save verify fail order,orderId:" + _productInfo.msg["order_no"] + " 商品类型：" + _productInfo.msg["product_type"]);
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
#if UNITY_ANDROID
                var value = _productInfo.msg["order_no"];
#else           
                var value = _productInfo.msg["order_id"];
#endif
                var want = list.Find(e => e.msg.ContainsValue(value));
                if (want == null)
                {
                    list.Add(_productInfo);
                    ES3.Save(_purchaseOrderMap, list);
                    ProcessLocalOrder();
                }
                else
                {
                    if (!inDealFailOrder)
                    {
                        inDealFailOrder = true;
                        InvokeRepeating(nameof(ProcessLocalOrder), 0, 30);
                    }
                }
            }
            else
            {
                List<DataConfig> tem = new List<DataConfig>() { };
                tem.Add(_productInfo);
                ES3.Save(_purchaseOrderMap, tem);
                ProcessLocalOrder();
                CoreLogUtils.Debug("[iap] IAP SaveVerifyFailOrderId 第一次存储表 添加失败订单 订单个数:" + tem.Count);
            }
        }

        // 校验成功后移除订单
        public void RemoveVerifyFailOrderId(string orderNo)
        {
            if (ES3.KeyExists(_purchaseOrderMap))
            {
                List<DataConfig> list = ES3.Load(_purchaseOrderMap) as List<DataConfig>;
                var want = list.Find(e => e.msg.ContainsValue(orderNo));
                if (want != null)
                {
                    CoreLogUtils.Debug("[iap] IAP RemoveVerifyFailOrderId 找到并移除订单，订单ID:" + orderNo);
                    list.Remove(want);
                    CoreIapEvents.SDKRemoveOrder(want.msg);
                    ES3.Save(_purchaseOrderMap, list);
                }

                CoreLogUtils.Debug("[iap] IAP 失败订单还剩余：" + list.Count + " 条未校验");
                if (list.Count > 0)
                {
                    ProcessLocalOrder();
                }
                else
                {
                    inDealFailOrder = false;
                    CancelInvoke(nameof(ProcessLocalOrder));
                }
            }
        }

        #endregion

        #region 添加商品
        public void AddProducts(Dictionary<string, ProductType> products)
        {
            mInitProductDic = products;

            FetchAdditionalProducts(products);
        }

        private void FetchAdditionalProducts(Dictionary<string, ProductType> ProductDic)
        {
            if (!IsInitialized())
            {
                mAddProductsDic = ProductDic;
                InitUnityPurchase();
                return;
            }

            if (IsFetchingAdditionalProducts)return;

            IsFetchingAdditionalProducts = true;
            if (ProductDic != null)
            {
                var additional = new HashSet<ProductDefinition>();
                foreach (string tID in ProductDic.Keys)
                {
                    additional.Add(new ProductDefinition(tID, ProductDic[tID]));
                }

                Action onSuccess = () =>
                {
                    IsFetchingAdditionalProducts = false;

                    if (OnGetProductsInfo != null)
                    {
                        OnGetProductsInfo(m_StoreController.products.all);
                    }

                };

                Action<InitializationFailureReason, string> onFailure = (InitializationFailureReason i, string msg) =>
                {
                    IsFetchingAdditionalProducts = false;
                };

                m_StoreController.FetchAdditionalProducts(additional, onSuccess, onFailure);
            }
        }
        #endregion

        #region  初始化
        public void PreInitialize()
        {
            if (!mServiceInit)
            {
                InitializeUnityServices(() =>
                {
                    mServiceInit = true;
                    CoreLogUtils.Debug("[iap] " + $"Unity Gaming Services init success.");

                }, (msg) =>
                {
                    CoreLogUtils.Debug("[iap] " + $"Unity Gaming Services failed to initialize with error: {msg}.");
                });
            }

        }

        private void InitializeUnityServices(Action onSuccess, Action<string> onError)
        {
            try
            {
                var options = new InitializationOptions().SetEnvironmentName(Environment);

                UnityServices.InitializeAsync(options).ContinueWith(task => onSuccess());
            }
            catch (Exception exception)
            {
                onError(exception.Message);
            }
        }
        public void Initialize()
        {
            if (m_StoreController == null && m_StoreExtensionProvider == null) InitUnityPurchase();

        }
        private bool IsInitialized()
        {
            return m_StoreController != null && m_StoreExtensionProvider != null;
        }
        private void InitUnityPurchase()
        {
            if (IsInitialized()) return;

            CoreAnalyticsMgr.TrackEvent("Purchase_Init");

            mRepeatCountDic = new Dictionary<string, int>();

            mIsRestoreClick = false;
            module ??= StandardPurchasingModule.Instance();
            builder ??= ConfigurationBuilder.Instance(module);

            int productsNum = 0;
            foreach (string tID in ProductDic.Keys)
            {
                productsNum++;
                if (!string.IsNullOrEmpty(tID))
                {
                    builder.AddProduct(tID, ProductDic[tID]);
                }
            }

            if (mInitProductDic != null && mInitProductDic.Count > 0)
            {
                foreach (string tID in mInitProductDic.Keys)
                {
                    productsNum++;
                    if (!string.IsNullOrEmpty(tID))
                    {
                        builder.AddProduct(tID, mInitProductDic[tID]);
                    }
                }
            }

            if (mAddProductsDic != null && mAddProductsDic.Count > 0)
            {
                foreach (string tID in mAddProductsDic.Keys)
                {
                    productsNum++;
                    if (!string.IsNullOrEmpty(tID))
                    {
                        builder.AddProduct(tID, mAddProductsDic[tID]);
                    }
                }
            }

            googlePlayConfiguration = builder.Configure<IGooglePlayConfiguration>();
            if (productsNum > 0)
            {
                //初始化;
                UnityPurchasing.Initialize(this, builder);
            }
            else
            {
                CoreLogUtils.Debug(
                    "[iap] UnityPurchasing will not initialize.Products is empty,please add product.");
            }
        }
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            CoreLogUtils.Debug("[iap] IAP initialize Succeed!");

            m_StoreController = controller;
            m_StoreExtensionProvider = extensions;
            if (OnGetProductsInfo != null)
            {
                OnGetProductsInfo(m_StoreController.products.all);
            }

            inDealFailOrder = false;
            ResetSaveOrderStatus();
            CoreAnalyticsMgr.TrackEvent("Purchase_Init_Success");
            CoreSdkMgr.OnPurchaseInitComplete?.Invoke(true,"");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            m_PurchaseInProgress = false;
            var res = new CoreLitePurchase.Result(CoreIapH5Mgr.Instance.currentOrderReq,CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.NativeApp);
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
            mIsRestoreClick = false;
            CoreIapEvents.IAPFail(mProductName,
                product.definition.id,
                "",
                "",
                product.metadata.isoCurrencyCode, 
                product.metadata.localizedPrice.ToString(), 
                mGameExtraParam,
                "Purchase failed callback reason:" + failureDescription.reason.ToString() + "msg:" + failureDescription.message,
    #if UNITY_ANDROID
                "Goolge",
    #else
                "Apple",
    #endif
                0
            );
            ProcessLocalOrder();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // 统一走两参数重载，埋点与回调在 OnInitializeFailed(error, message) 中执行（IDetailedStoreListener 时 Unity 会直接调两参数重载）
            OnInitializeFailed(error, "");
        }

#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
        public void OnInitializeFailed(InitializationFailureReason error, string? message)
#pragma warning restore CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
        {
            switch (error)
            {
                case InitializationFailureReason.AppNotKnown:
                    CoreLogUtils.Debug("[iap] Is your App correctly uploaded on the relevant publisher console?");
                    break;
                case InitializationFailureReason.PurchasingUnavailable:
                    CoreLogUtils.Debug("[iap] Billing disabled! Ask the user if billing is disabled in device settings.");
                    break;
                case InitializationFailureReason.NoProductsAvailable:
                    CoreLogUtils.Debug(
                        "[iap] No products available for purchase! Developer configuration error; check product metadata!");
                    break;
            }

            // IDetailedStoreListener 时 Unity IAP 会直接调用本两参数重载，不会走单参数重载，故在此补上埋点与回调
            CoreAnalyticsMgr.TrackEvent("Purchase_Init_Fail", new Dictionary<string, object> { { "fail_reason", error.ToString() }, { "message", message ?? "" } });
            CoreSdkMgr.OnPurchaseInitComplete?.Invoke(false, string.IsNullOrEmpty(message) ? error.ToString() : message);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureDescription)
        {
            m_PurchaseInProgress = false;
            var res = new CoreLitePurchase.Result(CoreIapH5Mgr.Instance.currentOrderReq,CoreIapH5Mgr.Instance.CreateFailQueryResultEntity(),false,false,CoreLitePaymentType.NativeApp);
            CoreSdkMgr.OnPurchaseComplete?.Invoke(res);
            mIsRestoreClick = false;
            CoreIapEvents.IAPFail(mProductName, 
                product.definition.id, 
                "",
                "",
                product.metadata.isoCurrencyCode, 
                product.metadata.localizedPriceString, 
                mGameExtraParam,
                "Purchase fail,reason：" + failureDescription.ToString(),
#if UNITY_ANDROID
                "Goolge",
#else
                "Apple",
#endif
                0
            );
            ProcessLocalOrder();
        }
        #endregion

        #region  Properties
        private StandardPurchasingModule module;
        private ConfigurationBuilder builder;
        public event Action<Product[]> OnGetProductsInfo;
        private IGooglePlayConfiguration googlePlayConfiguration;
        public event Action OnPurchaseBegin;
        private static string mProductName = "";
        private static string mGameExtraParam = "";
        public bool IsRestoring => mIsRestoring;
        private bool mIsRestoring = false;
        private static IStoreController m_StoreController; // 存储商品信息;
        private static IExtensionProvider m_StoreExtensionProvider; // IAP扩展工具;
        private bool m_PurchaseInProgress = false; // 是否处于付费中;
        /// <summary>原生内购收银台展示期间为 true，用于热启动开屏等暂停补量。</summary>
        public bool IsPurchaseInProgress => m_PurchaseInProgress;

        private Dictionary<string, ProductType> mInitProductDic;
        private const string Environment = "production";
        private bool mServiceInit = false;
        private bool IsFetchingAdditionalProducts = false;
        private Dictionary<string, ProductType> mAddProductsDic;
        private Dictionary<string, int> mRepeatCountDic;
        private bool mIsRestoreClick = false;
        #endregion

        #region Enum
        public enum DataState
        {
            // 未处理
            def,
            // 处理中
            deal
        }

        public class DataConfig
        {
            public Dictionary<string, string> msg;
            public DataState state;
        }
        #endregion
    }
}

