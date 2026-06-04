//
//  UUIDManager.m
//  Unity-iPhone
//
//  Created by fotoable on 2020/6/29.
//

#import "UUIDManager.h"
#import "KeyChainManager.h"

static NSString * const KEY_IN_KEYCHAIN = @"com.fotoable.WordVillas.uuid";

@implementation UUIDManager

+(void)setUUID:(NSString *)uuid
{
    if (uuid && uuid.length > 0)
    {
        [KeyChainManager save:KEY_IN_KEYCHAIN data:uuid];
    }
}

+(NSString *)getUUID
{
    NSString *uuid = [KeyChainManager load:KEY_IN_KEYCHAIN];
    
    return uuid;
}

+(void)deleteUUID
{
    [KeyChainManager delete:KEY_IN_KEYCHAIN];
}

@end
