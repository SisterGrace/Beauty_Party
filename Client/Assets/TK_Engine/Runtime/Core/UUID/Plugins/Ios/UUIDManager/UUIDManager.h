//
//  UUIDManager.h
//  Unity-iPhone
//
//  Created by fotoable on 2020/6/29.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface UUIDManager : NSObject

+(void)setUUID:(NSString *)uuid;

+(NSString *)getUUID;

+(void)deleteUUID;

@end

NS_ASSUME_NONNULL_END
