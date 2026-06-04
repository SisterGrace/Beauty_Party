#import <Foundation/Foundation.h>
#import <MessageUI/MessageUI.h>
#import <UIKit/UIKit.h>
#import <StoreKit/StoreKit.h>

#define SCREENSIZE_IS_8P ([UIScreen instancesRespondToSelector:@selector(currentMode)] ? CGSizeEqualToSize(CGSizeMake(1242,2208), [[UIScreen mainScreen] currentMode].size) && !UI_IS_IPAD : NO)
#define UI_IS_IPAD (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad)

#define SCREENSIZE_IS_XS ([UIScreen instancesRespondToSelector:@selector(currentMode)] ? CGSizeEqualToSize(CGSizeMake(1125, 2436), [[UIScreen mainScreen] currentMode].size) && !UI_IS_IPAD : NO)

char * MakeStringCopy (const char* string)
{
    
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    
    strcpy(res, string);
    
    return res;
    
}

// C 函数接口
extern "C" {

    typedef struct {
        float x;
        float y;
        float width;
        float height;
    } UnityRect;

    void TransformationToOCCoordinates_Out(
        float inX, float inY, float inWidth, float inHeight,
        float* outX, float* outY, float* outWidth, float* outHeight)
    {
        CGFloat scales = [UIScreen mainScreen].scale;
        CGFloat originX = inX, originY = inY, originW = inWidth, originH = inHeight;

        if (SCREENSIZE_IS_8P) {
            *outX = originX / 0.87 / scales;
            *outY = originY / 0.87 / scales;
            *outWidth  = originW / 0.87 / scales;
            *outHeight = originH / 0.87 / scales;
        } else if (SCREENSIZE_IS_XS) {
            *outX = originX / 0.96 / scales;
            *outY = originY / 0.96 / scales;
            *outWidth  = originW / 0.96 / scales;
            *outHeight = originH / 0.96 / scales;
        } else {
            *outX = originX / scales;
            *outY = originY / scales;
            *outWidth  = originW / scales;
            *outHeight = originH / scales;
        }
    }
    
    int IsiPhone()
    {
        // 使用UIUserInterfaceIdiom来判断设备类型
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone) {
            return 1; // 是iPhone
        } else {
            return 0; // 不是iPhone（可能是iPad等）
        }
    }
    
    // 获取屏幕缩放因子
    float GetScreenScale()
    {
        // 返回主屏幕的缩放因子
        return [UIScreen mainScreen].scale;
    }

    char * GetCountryCode()
    {
        if (@available(iOS 13.0, *)) {
            SKStorefront *storefront = [[SKPaymentQueue defaultQueue] storefront];
            if (storefront) {
                NSString *storeCountryCode = storefront.countryCode;
                return MakeStringCopy([storeCountryCode UTF8String]);
                // 使用 storeCountryCode 进行你的逻辑处理
            } else {
                return MakeStringCopy([@"" UTF8String]);
            }
        } else
        {
            return MakeStringCopy([@"" UTF8String]);
        }
    }

    void _EmailComposer_ShowMailComposer(const char* to, const char* subject, const char* body) {
        
        // 检查是否支持发送邮件
        if (![MFMailComposeViewController canSendMail]) {
            NSLog(@"[EmailComposer] Device cannot send mail");
            
            // iOS无法发送邮件时，回退到mailto链接
            NSString *mailtoLink = [NSString stringWithFormat:@"mailto:%s?subject=%s&body=%s", 
                                   to, subject, body];
            NSURL *url = [NSURL URLWithString:[mailtoLink stringByAddingPercentEncodingWithAllowedCharacters:[NSCharacterSet URLQueryAllowedCharacterSet]]];
            
            dispatch_async(dispatch_get_main_queue(), ^{
                [[UIApplication sharedApplication] openURL:url options:@{} completionHandler:nil];
            });
            
            return;
        }
        
        // 获取当前视图控制器
        UIViewController *rootViewController = nil;
        
        // 尝试获取Unity的视图控制器
        rootViewController = UnityGetGLViewController();
        
        if (!rootViewController) {
            // 如果获取失败，尝试从主窗口获取
            if (@available(iOS 13.0, *)) {
                UIWindowScene *windowScene = (UIWindowScene*)[UIApplication sharedApplication].connectedScenes.anyObject;
                if (windowScene) {
                    for (UIWindow *window in windowScene.windows) {
                        if (window.isKeyWindow) {
                            rootViewController = window.rootViewController;
                            break;
                        }
                    }
                }
            } else {
                // iOS 13以下版本
                rootViewController = [[UIApplication sharedApplication] keyWindow].rootViewController;
            }
        }
        
        if (!rootViewController) {
            NSLog(@"[EmailComposer] Cannot find root view controller");
            return;
        }
        
        // 创建邮件 composer
        MFMailComposeViewController *mailComposer = [[MFMailComposeViewController alloc] init];
        
        // 设置收件人
        if (to && strlen(to) > 0) {
            NSString *toStr = [NSString stringWithUTF8String:to];
            [mailComposer setToRecipients:@[toStr]];
        }
        
        // 设置主题
        if (subject && strlen(subject) > 0) {
            NSString *subjectStr = [NSString stringWithUTF8String:subject];
            [mailComposer setSubject:subjectStr];
        }
        
        // 设置正文
        if (body && strlen(body) > 0) {
            NSString *bodyStr = [NSString stringWithUTF8String:body];
            [mailComposer setMessageBody:bodyStr isHTML:NO];
        }
        
        // 展示邮件 composer
        dispatch_async(dispatch_get_main_queue(), ^{
            [rootViewController presentViewController:mailComposer animated:YES completion:nil];
        });
    }
}
