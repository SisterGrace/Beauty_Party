//
//  ZdkBridge.m
//  Unity-iPhone
//
//  Created by PhantomJ on 2020/1/2.
//

#import "UUIDManager.h"
#import "UnityAppController.h"

#import <Foundation/Foundation.h>

@interface UUIDManager (CPart)
{
    
}
@end

@implementation UUIDManager (CPart)

extern "C"
{
    NSString* cStr2NSStr(const char* string)
    {
        NSString* tNSStr = [NSString stringWithUTF8String:string];
        
        return tNSStr;
    }
    
    char* MakeStringCopy(const char* string)
    {
        if(string == NULL)
            return NULL;
        char* res = (char*)malloc(strlen(string) + 1);
        
        strcpy(res, string);
        
        return res;
    }
    
    const void mSetUUID(const char* pUUID)
    {
        [UUIDManager setUUID:cStr2NSStr(pUUID)];
    }
    
    const char* mGetUUID()
    {
        NSString* uuid = [UUIDManager getUUID];
        if (uuid && uuid.length > 0)
        {
            return MakeStringCopy([uuid UTF8String]);
        }
        
        return NULL;
    }
    
    const void mDeleteUUID()
    {
        [UUIDManager deleteUUID];
    }
}

@end
